using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ShearWallVisualizer
{
    [Flags]
    public enum ChangeType
    {
        Redraw = 1,
        Resize = 2,
        Scroll = 4,
    }

    public enum LayerType
    {
        Background = 1,
        Wall = 2,
        Diaphragm = 4,
        Load = 8,
        Results = 16,
        Dimension = 32,
        Cursor = 64

    }

    public class LayerManager : FrameworkElement
    {
        private readonly VisualCollection m_children;
        private readonly List<LayerData> m_layers = new List<LayerData>();

        public LayerManager()
        {
            m_children = new VisualCollection(this);
        }

        public void AddLayer(int priority, Action<DrawingContext> draw, ChangeType notifyOnChange = ChangeType.Redraw)
        {
            var drawingVisual = new DrawingVisual();

            var layerInfo = new LayerData(priority, draw, drawingVisual, notifyOnChange);
            m_layers.Add(layerInfo);

            //Sort the layers by priority
            m_layers.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            //Remove all the visual layers and add them in order
            m_children.Clear();
            m_layers.ForEach(l => m_children.Add(l.Visual));
        }

        public void Draw(ChangeType change)
        {
            var affected = from l in m_layers
                           where ((change & ChangeType.Redraw) != 0) || ((l.NotifyOnChange & change) != 0)
                           orderby l.Priority
                           select l;

            foreach (LayerData layer in affected)
            {
                DrawingContext ctx = layer.Visual.RenderOpen();
                layer.Draw(ctx);
                ctx.Close();
            }
        }

        protected override int VisualChildrenCount
        {
            get { return m_children.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= m_children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return m_children[index];
        }
    }
}
