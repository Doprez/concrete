using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

namespace Concrete;

public static unsafe class MetricsWindow
{
    public static void Draw(float deltaTime)
    {
        ImGui.Begin("\ue473 Metrics", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar);

        // frametime graph
        if (ImPlot.BeginPlot("frametime: " + (int)Metrics.averageFrameTime + "ms", new(-1, 128), ImPlotFlags.NoLegend | ImPlotFlags.NoMouseText | ImPlotFlags.NoInputs | ImPlotFlags.NoFrame))
        {
            // setup axis
            var xflags = ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoGridLines;
            var yflags = ImPlotAxisFlags.NoLabel;
            ImPlot.SetupAxes("frame", "time (ms)", xflags, yflags);
            ImPlot.SetupAxesLimits(0, Metrics.framesToCheck, 0, 40);
            
            // plot frames when ready
            if (Metrics.dataIsReady)
            {
                ImPlot.PlotLine("frametime", ref Metrics.lastFrameTimes[0], Metrics.framesToCheck, ImPlotLineFlags.Shaded);
            }

            ImPlot.EndPlot();
        }

        // framerate graph
        if (ImPlot.BeginPlot("framerate: " + (int)Metrics.averageFrameRate + "fps", new(-1, 128), ImPlotFlags.NoLegend | ImPlotFlags.NoMouseText | ImPlotFlags.NoInputs | ImPlotFlags.NoFrame))
        {
            // setup axis
            var xflags = ImPlotAxisFlags.NoLabel | ImPlotAxisFlags.NoTickLabels | ImPlotAxisFlags.NoTickMarks | ImPlotAxisFlags.NoGridLines;
            var yflags = ImPlotAxisFlags.NoLabel;
            ImPlot.SetupAxes("frame", "rate (fps)", xflags, yflags);
            ImPlot.SetupAxesLimits(0, Metrics.framesToCheck, 0, 512);
            
            // plot frames when ready
            if (Metrics.dataIsReady)
            {
                ImPlot.PlotLine("framerate", ref Metrics.lastFrameRates[0], Metrics.framesToCheck, ImPlotLineFlags.Shaded);
            }

            ImPlot.EndPlot();
        }

        ImGui.End();
    }
}