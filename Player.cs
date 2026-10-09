using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TileQuest
{
    public enum FacingDirection { Up, Down, Left, Right }

    // Pokemon-style movement: input is grid-locked (one tile per step, four
    // directions only) but animated with a smooth slide rather than an instant
    // snap. While a slide is in progress, no new input is accepted — matching
    // the classic overworld feel.
    public class Player
    {
        public Point GridPosition { get; private set; }
        public Vector2 PixelPosition { get; private set; }
        public FacingDirection Facing { get; private set; } = FacingDirection.Down;
        public bool IsMoving { get; private set; }

        // Walk animation state. IsWalking stays true across the one-frame gap
        // between back-to-back steps (key held down), so the animation runs
        // continuously instead of restarting every tile. AnimationTime is the
        // number of seconds spent walking since the last time the player was
        // standing still.
        public bool IsWalking { get; private set; }
        public float AnimationTime { get; private set; }

        // Melee swing. The player stands still and swings at whatever is in the
        // tiles in front: SwingWidth tiles across (centred on the tile directly
        // ahead) and SwingDepth tiles out. 3 x 1 is the front tile plus the one
        // on each side of it. Change the two constants to reshape the swing.
        public const int SwingWidth = 3;
        public const int SwingDepth = 1;

        public bool IsAttacking { get; private set; }

        // 0 -> 1 across the swing; used by the renderer to sweep the effect.
        public float AttackProgress { get; private set; }

        public event Action<Point>? OnTileEntered;

        // Fired once per swing, at the moment the blow lands, with the tiles it
        // covers. The game decides what is standing in them and what happens.
        public event Action<IReadOnlyList<Point>>? OnSwingImpact;

        private Vector2 _targetPixelPosition;
        private readonly float _speed; // pixels per second
        private readonly int _tileSize;
        private float _idleSeconds;
        private const float StopWalkingAfterSeconds = 0.06f;
        private const float SwingSeconds = 0.28f;
        private const float SwingImpactAt = 0.35f; // fraction of the swing when the blow lands
        private const float SwingCooldownSeconds = 0.12f;
        private const float AttackBufferSeconds = 0.2f; // a press this recently still counts once free
        private float _attackBufferRemaining;
        private float _swingElapsed;
        private float _swingCooldownRemaining;
        private bool _swingImpactDone;

        public Player(Point startGridPos, int tileSize, float tilesPerSecond = 4f)
        {
            GridPosition = startGridPos;
            _tileSize = tileSize;
            PixelPosition = new Vector2(startGridPos.X * tileSize, startGridPos.Y * tileSize);
            _targetPixelPosition = PixelPosition;
            _speed = tilesPerSecond * tileSize;
        }

        public void Teleport(Point gridPosition)
        {
            GridPosition = gridPosition;
            PixelPosition = new Vector2(gridPosition.X * _tileSize, gridPosition.Y * _tileSize);
            _targetPixelPosition = PixelPosition;
            IsMoving = false;
            IsWalking = false;
            AnimationTime = 0f;
            _idleSeconds = 0f;
            IsAttacking = false;
            AttackProgress = 0f;
            _attackBufferRemaining = 0f;
        }

        public Point FacingVector => Facing switch
        {
            FacingDirection.Up => new Point(0, -1),
            FacingDirection.Down => new Point(0, 1),
            FacingDirection.Left => new Point(-1, 0),
            _ => new Point(1, 0),
        };

        // The tiles the current swing covers, ordered across the swing from one
        // side to the other (so the renderer can sweep along them).
        public IReadOnlyList<Point> GetSwingTiles()
        {
            Point forward = FacingVector;
            Point side = new Point(-forward.Y, forward.X);
            var tiles = new List<Point>();
            for (int depth = 1; depth <= SwingDepth; depth++)
            {
                for (int offset = -(SwingWidth / 2); offset <= SwingWidth / 2; offset++)
                {
                    tiles.Add(new Point(
                        GridPosition.X + forward.X * depth + side.X * offset,
                        GridPosition.Y + forward.Y * depth + side.Y * offset));
                }
            }

            return tiles;
        }

        // attackPressed is true on the frame the attack key goes down. isTileOccupied
        // lets the game block movement into tiles held by something other than
        // terrain (enemies).
        public void Update(
            GameTime gameTime,
            TileMap map,
            KeyboardState keyboard,
            bool attackPressed = false,
            Func<Point, bool>? isTileOccupied = null)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_swingCooldownRemaining > 0f)
            {
                _swingCooldownRemaining = Math.Max(0f, _swingCooldownRemaining - deltaSeconds);
            }

            // Remember a recent press so one made mid-step (or mid-swing) isn't lost.
            _attackBufferRemaining = attackPressed
                ? AttackBufferSeconds
                : Math.Max(0f, _attackBufferRemaining - deltaSeconds);

            // Committed to the swing: no walking until it finishes.
            if (IsAttacking)
            {
                UpdateSwing(deltaSeconds);
                return;
            }

            if (IsMoving)
            {
                AnimationTime += deltaSeconds;
                _idleSeconds = 0f;
                SlideTowardTarget(gameTime);
                return;
            }

            // Grid-locked like movement: a swing can only start between steps.
            if (_attackBufferRemaining > 0f && _swingCooldownRemaining <= 0f)
            {
                StartSwing();
                return;
            }

            var direction = ReadDirection(keyboard);
            if (direction == Point.Zero)
            {
                RegisterStandingStill(deltaSeconds);
                return;
            }

            var candidate = new Point(GridPosition.X + direction.X, GridPosition.Y + direction.Y);
            if (!map.IsWalkable(candidate) || (isTileOccupied?.Invoke(candidate) ?? false))
            {
                // Bump into the wall: still update facing (classic Pokemon feel —
                // you turn to face a wall even if you can't walk into it) but
                // don't start a slide.
                RegisterStandingStill(deltaSeconds);
                return;
            }

            GridPosition = candidate;
            _targetPixelPosition = new Vector2(candidate.X * _tileSize, candidate.Y * _tileSize);
            IsMoving = true;
            IsWalking = true;
            _idleSeconds = 0f;
        }

        private void StartSwing()
        {
            _attackBufferRemaining = 0f;
            IsAttacking = true;
            AttackProgress = 0f;
            _swingElapsed = 0f;
            _swingImpactDone = false;
            IsWalking = false;
            AnimationTime = 0f;
            _idleSeconds = 0f;
        }

        private void UpdateSwing(float deltaSeconds)
        {
            _swingElapsed += deltaSeconds;
            AttackProgress = Math.Min(1f, _swingElapsed / SwingSeconds);

            if (!_swingImpactDone && AttackProgress >= SwingImpactAt)
            {
                _swingImpactDone = true;
                OnSwingImpact?.Invoke(GetSwingTiles());
            }

            if (AttackProgress >= 1f)
            {
                IsAttacking = false;
                AttackProgress = 0f;
                _swingCooldownRemaining = SwingCooldownSeconds;
            }
        }

        // Only switch back to the standing pose once the player has really
        // stopped, not during the single frame between two chained steps.
        private void RegisterStandingStill(float deltaSeconds)
        {
            _idleSeconds += deltaSeconds;
            if (_idleSeconds >= StopWalkingAfterSeconds)
            {
                IsWalking = false;
                AnimationTime = 0f;
            }
        }

        private void SlideTowardTarget(GameTime gameTime)
        {
            Vector2 toTarget = _targetPixelPosition - PixelPosition;
            float distance = toTarget.Length();
            float step = _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (step >= distance)
            {
                PixelPosition = _targetPixelPosition;
                IsMoving = false;
                OnTileEntered?.Invoke(GridPosition);
            }
            else
            {
                toTarget.Normalize();
                PixelPosition += toTarget * step;
            }
        }

        private Point ReadDirection(KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            {
                Facing = FacingDirection.Up;
                return new Point(0, -1);
            }
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            {
                Facing = FacingDirection.Down;
                return new Point(0, 1);
            }
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            {
                Facing = FacingDirection.Left;
                return new Point(-1, 0);
            }
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            {
                Facing = FacingDirection.Right;
                return new Point(1, 0);
            }
            return Point.Zero;
        }
    }
}