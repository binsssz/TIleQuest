using System;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // A dedicated camera: tracks a target (typically the player's pixel
    // position), clamps itself to the map's pixel bounds so dead space past
    // the edges is never visible, and can either smoothly follow or snap
    // instantly to a position.
    public class Camera2D
    {
        public Vector2 Position { get; private set; }

        private readonly int _viewportWidth;
        private readonly int _viewportHeight;
        private readonly int _mapPixelWidth;
        private readonly int _mapPixelHeight;

        public Camera2D(int viewportWidth, int viewportHeight, int mapWidthInTiles, int mapHeightInTiles, int tileSize)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            _mapPixelWidth = mapWidthInTiles * tileSize;
            _mapPixelHeight = mapHeightInTiles * tileSize;
        }

        // Smoothly moves the camera toward centering on `target`. followSpeed
        // is how aggressively it closes the remaining gap each second (higher
        // = snappier, lower = laggier/more cinematic). Uses exponential decay
        // rather than a plain per-frame Vector2.Lerp so the motion looks the
        // same regardless of frame rate.
        public void Update(GameTime gameTime, Vector2 target, float followSpeed = 8f)
        {
            var desired = ClampToWorld(CenterOn(target));
            float t = 1f - MathF.Exp(-followSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
            Position = Vector2.Lerp(Position, desired, t);
        }

        // Instantly centers the camera on `target`, ignoring follow speed.
        // Call this once right after spawning the player so the camera
        // doesn't visibly slide in from (0, 0) on the very first frame.
        public void SnapTo(Vector2 target)
        {
            Position = ClampToWorld(CenterOn(target));
        }

        // The matrix to pass into SpriteBatch.Begin(transformMatrix: ...) so
        // everything drawn in world space appears correctly offset by the
        // camera.
        public Matrix GetTransformationMatrix()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
        }

        private Vector2 CenterOn(Vector2 target)
        {
            return target - new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);
        }

        // Keeps the camera's top-left corner within [0, mapPixelSize - viewportSize]
        // on each axis, so the view never shows space beyond the map. When the
        // map is smaller than the viewport on an axis, the clamp collapses to
        // 0 (centering isn't attempted — the whole map just fits with margin).
        private Vector2 ClampToWorld(Vector2 topLeft)
        {
            float maxX = MathF.Max(0, _mapPixelWidth - _viewportWidth);
            float maxY = MathF.Max(0, _mapPixelHeight - _viewportHeight);
            return new Vector2(
                MathHelper.Clamp(topLeft.X, 0, maxX),
                MathHelper.Clamp(topLeft.Y, 0, maxY));
        }
    }
}
