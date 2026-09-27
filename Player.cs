using System;
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

        public event Action<Point>? OnTileEntered;

        private Vector2 _targetPixelPosition;
        private readonly float _speed; // pixels per second
        private readonly int _tileSize;

        public Player(Point startGridPos, int tileSize, float tilesPerSecond = 4f)
        {
            GridPosition = startGridPos;
            _tileSize = tileSize;
            PixelPosition = new Vector2(startGridPos.X * tileSize, startGridPos.Y * tileSize);
            _targetPixelPosition = PixelPosition;
            _speed = tilesPerSecond * tileSize;
        }

        public void Update(GameTime gameTime, TileMap map, KeyboardState keyboard)
        {
            if (IsMoving)
            {
                SlideTowardTarget(gameTime);
                return;
            }

            var direction = ReadDirection(keyboard);
            if (direction == Point.Zero)
            {
                return;
            }

            var candidate = new Point(GridPosition.X + direction.X, GridPosition.Y + direction.Y);
            if (!map.IsWalkable(candidate))
            {
                // Bump into the wall: still update facing (classic Pokemon feel —
                // you turn to face a wall even if you can't walk into it) but
                // don't start a slide.
                return;
            }

            GridPosition = candidate;
            _targetPixelPosition = new Vector2(candidate.X * _tileSize, candidate.Y * _tileSize);
            IsMoving = true;
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
