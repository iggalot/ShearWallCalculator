﻿using System;
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
        public List<ImageLayer> ImageLayers { get; } = new List<ImageLayer>();
        public ImageLayer currentReferenceImageLayer { get; private set; }
        public string currentReferenceImagePath { get; private set; }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">path to the image file</param>
        /// <param name="scale_x">the pixel to world scale factor in x-dir</param>
        /// <param name="scale_y">the pixel to world scale factor in y-dir</param>
        public void AddImageLayer(string path, double scale_x=1, double scale_y=1)
        {
            if (ImageLayers.Count > 0)
            {
                RemoveImageLayer(0);
            }

            currentReferenceImagePath = path;
            currentReferenceImageLayer = new ImageLayer(path);
            currentReferenceImageLayer.Resize(
                currentReferenceImageLayer.TargetRect.Width * scale_x,
                currentReferenceImageLayer.TargetRect.Height * scale_y); // resize it to the new scale

            ImageLayers.Add(currentReferenceImageLayer);
            m_children.Add(currentReferenceImageLayer.Visual);
        }

        public void RemoveImageLayer(int index)
        {
            if (index >= 0 && index < ImageLayers.Count)
            {
                var layer = ImageLayers[index];

                // Remove visual from VisualCollection
                if (layer.Visual != null)
                {
                    m_children.Remove(layer.Visual);
                }

                // Remove from internal list
                ImageLayers.RemoveAt(index);
            }
        }

        public void ResizeImageLayer(int index, double width, double height)
        {
            if (index >= 0 && index < ImageLayers.Count)
            {
                ImageLayers[index].Resize(width, height);
            }
        }

        public void MoveImageLayer(int index, double x, double y)
        {
            if (index >= 0 && index < ImageLayers.Count)
            {
                ImageLayers[index].SetPosition(x, y);
            }
        }
    }
}
