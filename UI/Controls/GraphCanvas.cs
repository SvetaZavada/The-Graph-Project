﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphAlgorithms.VerticeLocation;
using GraphDataLayer;
using UI.Infrastructure;

namespace UI.Controls
{
    public class GraphCanvas : Panel
    {
        public static DependencyProperty SelectedVerticeIndexProperty =
            DependencyProperty.Register("SelectedVerticeIndex", typeof (int), typeof (GraphCanvas),
                new FrameworkPropertyMetadata(-1));

        public int SelectedVerticeIndex
        {
            get { return (int) GetValue(SelectedVerticeIndexProperty); }
            set { SetValue(SelectedVerticeIndexProperty, value); }
        }

        public static DependencyProperty GraphProperty = DependencyProperty.Register("Graph", typeof (NamedGraph),
            typeof (GraphCanvas), new FrameworkPropertyMetadata(null, GraphChanded));

        private static void GraphChanded(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var graphCanvas = dependencyObject as GraphCanvas;
            if (graphCanvas?.Graph == null)
                return;
            graphCanvas.CreateGraphMathModel();
            graphCanvas.RelocateGraph();
        }

        public NamedGraph Graph
        {
            get { return (NamedGraph) GetValue(GraphProperty); }
            set { SetValue(GraphProperty, value); }
        }

        public static DependencyProperty VisitedPathProperty = DependencyProperty.Register("VisitedPath",
            typeof (IEnumerable<int>), typeof (GraphCanvas),
            new FrameworkPropertyMetadata(new List<int>(), VisitedPathChanged));

        private static void VisitedPathChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var graphCanvas = dependencyObject as GraphCanvas;
            if (graphCanvas == null)
                return;
            graphCanvas.SetChildrenDefaultViews();
            graphCanvas.PickOutPath();
            graphCanvas.UpdateChildrenView();
        }

        public int[] VisitedPath
        {
            get { return (int[]) GetValue(VisitedPathProperty); }
            set { SetValue(VisitedPathProperty, value); }
        }

        public static DependencyProperty IsOpenedCustomMenuProperty =
            DependencyProperty.Register("IsOpenedCustomMeun", typeof (bool), typeof (GraphCanvas));

        public bool IsOpenedCustomMeun
        {
            get { return (bool) GetValue(IsOpenedCustomMenuProperty); }
            set { SetValue(IsOpenedCustomMenuProperty, value); }
        }

        private Point center;

        private double comprasionRatio;

        private double verticesScale;

        private List<NodeView> nodes;

        private List<ArrowView> arrows;

        public IVerticesLocator VerticesLocator { get; } = new ForceVerticesLocator();

        private void CreateGraphMathModel()
        {
            VerticesLocator.Clear();
            var vertices = Enumerable.Range(0, Graph.VerticesCount)
                .Select(i => new Node(i))
                .ToArray();
            for (var i = 0; i < vertices.Length; i++)
            {
                foreach (var neighbour in Graph.GetNeighbours(i))
                {
                    vertices[i].AddChild(vertices[neighbour]);
                }
                VerticesLocator.AddNode(vertices[i]);
            }
        }

        private void RelocateGraph()
        {
            Children.Clear();

            if (VerticesLocator == null)
                return;
            VerticesLocator.Locate();

            nodes = VerticesLocator.Nodes.Select(n => new NodeView(n.Number, Graph[n.Number])
            {
                Center = new Point(n.Location.X, n.Location.Y)
            }).ToList();
            arrows = new List<ArrowView>(Graph.ArrowsCount);

            for (var i = 0; i < nodes.Count; i++)
            {
                SetZIndex(nodes[i], 10);

                foreach (var connection in Graph.GetNeighbours(i))
                {
                    var line = new ArrowView(nodes[i], nodes[connection]);
                    Children.Add(line);
                    nodes[i].Arrows.Add(line);
                    nodes[connection].Arrows.Add(line);
                    arrows.Add(line);
                }
                Children.Add(nodes[i]);
            }
        }

        private void SetChildrenDefaultViews()
        {
            foreach (IGraphObject child in Children)
            {
                child.ChangeViewToDefault();
            }
        }

        private void PickOutPath()
        {
            if (VisitedPath == null)
                return;
            for (var i = 0; i < VisitedPath.Length; i++)
            {
                var nextNode = i + 1 != VisitedPath.Length ? nodes[VisitedPath[i + 1]] : null;
                nodes[VisitedPath[i]].IncludeView(nextNode);
            }
        }

        private void UpdateChildrenView()
        {
            foreach (IGraphObject child in Children)
            {
                child.ChangeView();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            CalculateCurrentVerticeParameters(availableSize);

            foreach (UIElement child in Children)
            {
                if (child is NodeView)
                {
                    var node = child as NodeView;
                    node.Radius = verticesScale/2;
                }
                child.Measure(availableSize);
            }
            return availableSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement child in Children)
            {
                if (child is NodeView)
                {
                    ArrangeNode(child as NodeView);
                }
                else if (child is ArrowView)
                {
                    ArrangeArrow(child as ArrowView, arrangeSize);
                }
            }
            return arrangeSize;
        }

        private void CalculateCurrentVerticeParameters(Size arrangeSize)
        {
            center = new Point(arrangeSize.Width/2, arrangeSize.Height/2);
            verticesScale = CalculateVerticeScale();
            comprasionRatio = CalculateComprasionRatio(arrangeSize);
        }

        private void ArrangeNode(NodeView node)
        {
            node.Radius = verticesScale/2;
            var topLeftPoint = CalcShiftFor(node.Center, verticesScale/2);
            node.Arrange(new Rect(node.ShiftPointFromTitle(topLeftPoint), node.DesiredSize));
        }

        private void ArrangeArrow(ArrowView arrowView, Size arrangeSize)
        {
            var startArrowPoint = CalcShiftFor(arrowView.StartNode.Center);
            var endArrowPoint = CalcShiftFor(arrowView.EndNode.Center);
            arrowView.SetCanvasParameters(startArrowPoint, endArrowPoint, verticesScale);

            arrowView.Arrange(new Rect(new Point(), arrangeSize));
        }

        private double CalculateVerticeScale()
        {
            var side = Math.Min(ActualHeight, ActualWidth);
            var verticesCount = VerticesLocator?.Nodes.Count ?? 0;
            return Math.Min(side/verticesCount, 40.0);
        }

        private double CalculateComprasionRatio(Size arrangeSize)
        {
            if (VerticesLocator == null)
                return 1;
            var verticalScaleFactor = (arrangeSize.Height - 2*verticesScale - 20)/VerticesLocator.Size.Height;
            var horizontalScaleFactor = (arrangeSize.Width - 2*verticesScale - 20)/VerticesLocator.Size.Width;
            return Math.Min(verticalScaleFactor, horizontalScaleFactor);
        }

        private Point CalcShiftFor(Point point, double shift = 0.0D)
        {
            return new Point(center.X + point.X*comprasionRatio - shift,
                center.Y - point.Y*comprasionRatio - shift);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SetChildrenDefaultViews();
            SelectedVerticeIndex = -1;
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsOpenedCustomMeun = false;
            var nodeView = e.Source as NodeView;
            if (nodeView != null)
            {
                SelectedVerticeIndex = nodeView.Number;
                UpdateChildrenView();
            }
            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            IsOpenedCustomMeun = !IsOpenedCustomMeun;
            base.OnMouseRightButtonDown(e);
        }
    }
}
