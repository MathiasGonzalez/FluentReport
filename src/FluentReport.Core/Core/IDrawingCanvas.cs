using FluentReport.Styling;

namespace FluentReport.Core;

/// <summary>
/// Abstracts drawing operations. Extends <see cref="ITextMeasurer"/> so the canvas
/// can also measure text during the render phase.
/// </summary>
public interface IDrawingCanvas : ITextMeasurer
{
    // ── State management ────────────────────────────────────────────────────
    void Save();
    void Restore();
    void ClipRect(float x, float y, float width, float height);

    // ── Drawing primitives ───────────────────────────────────────────────────
    void DrawLine(float x0, float y0, float x1, float y1, ReportColor color, float strokeWidth);
    void DrawFilledRect(float x, float y, float width, float height, ReportColor color);
    void DrawStrokedRect(float x, float y, float width, float height, ReportColor color, float strokeWidth);
    void DrawText(string text, float x, float y, DrawTextAlign align, TextStyle style);

    /// <summary>Draws image data (PNG, JPEG, etc.) from raw bytes into the given bounds.</summary>
    void DrawImageBytes(byte[] bytes, float x, float y, float width, float height);

    void DrawCircle(float x, float y, float radius, ReportColor color);
    void DrawPolyline(IReadOnlyList<(float X, float Y)> points, ReportColor color, float strokeWidth);
}
