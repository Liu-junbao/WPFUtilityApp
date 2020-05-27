using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Wpf.Utilities.Controls
{
    /// <summary>
    /// 虚拟容器
    /// </summary>
    public class VirtzPanel : VirtualizingPanel, IVirtzPanel, IScrollInfo
    {
        #region Fields And Constructors
        public static readonly DependencyProperty ScrollHorizontalOffsetProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollHorizontalOffset), typeof(double), typeof(VirtzPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure, OnPropertyChanged));
        public static readonly DependencyProperty ScrollVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollVerticalOffset), typeof(double), typeof(VirtzPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure, OnPropertyChanged));
        public static readonly DependencyProperty ScrollExtentWidthProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollExtentWidth), typeof(double), typeof(VirtzPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure, OnPropertyChanged));
        public static readonly DependencyProperty ScrollExtentHeightProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollExtentHeight), typeof(double), typeof(VirtzPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure, OnPropertyChanged));
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtzPanel panel)
            {
                if (e.Property == ScrollHorizontalOffsetProperty || e.Property == ScrollVerticalOffsetProperty)
                {
                    panel._scrollOwner?.InvalidateScrollInfo();
                }
                else if (e.Property == ScrollExtentWidthProperty)
                {
                    var extentWidth = (double)e.NewValue;
                    var viewport = panel.ScrollViewport;
                    var offsetX = panel.ScrollHorizontalOffset;
                    if (extentWidth < viewport.Width + offsetX)//超出显示范围不再触发更新
                    {
                        panel._scrollOwner?.InvalidateScrollInfo();
                    }
                }
                else if (e.Property == ScrollExtentHeightProperty)
                {
                    var extentHeight = (double)e.NewValue;
                    var viewport = panel.ScrollViewport;
                    var offsetY = panel.ScrollVerticalOffset;
                    if (extentHeight < viewport.Height + offsetY)
                    {
                        panel._scrollOwner?.InvalidateScrollInfo();
                    }
                }
            }
        }

        public readonly double ScrollLineDelta;
        public readonly double MouseWheelDelta;
        public readonly int MouseWheelDeltaItem;
        private ScrollViewer _scrollOwner;
        private DependencyObject _itemsOwner;
        private IVirtzPanelOwner _virtzPanelOwner;
        private IRecyclingItemContainerGenerator _itemContainerGenerator;
        private Size _firstChildDesiredSize;
        public VirtzPanel(double scrollLineDelta, double mouseWheelDelta, int mouseWheelDeltaItem)
        {
            ScrollLineDelta = scrollLineDelta;//16.0
            MouseWheelDelta = mouseWheelDelta;//48.0
            MouseWheelDeltaItem = mouseWheelDeltaItem;//3
        }
        public VirtzPanel() : this(16.0, 48.0, 3) { }
        #endregion

        #region Properties
        protected ReadOnlyCollection<object> Items => ((ItemContainerGenerator)ItemContainerGenerator).Items;
        protected override bool CanHierarchicallyScrollAndVirtualizeCore => true;
        protected ScrollUnit ScrollUnit => GetScrollUnit(ItemsControl);
        protected bool IsVirtualizing => GetIsVirtualizing(ItemsControl);
        protected VirtualizationMode VirtualizationMode => GetVirtualizationMode(ItemsControl);
        protected bool IsRecycling => VirtualizationMode == VirtualizationMode.Recycling;
        protected VirtualizationCacheLength CacheLength { get; private set; }
        protected VirtualizationCacheLengthUnit CacheLengthUnit { get; private set; }
        protected ItemsControl ItemsControl => ItemsControl.GetItemsOwner(this);
        protected DependencyObject ItemsOwner
        {
            get
            {
                if (_itemsOwner == null)
                {
                    /* Use reflection to access internal method because the public 
                     * GetItemsOwner method does always return the itmes control instead 
                     * of the real items owner for example the group item when grouping */
                    _itemsOwner = (DependencyObject)typeof(ItemsControl).GetMethod(
                       "GetItemsOwnerInternal",
                       BindingFlags.Static | BindingFlags.NonPublic,
                       null,
                       new Type[] { typeof(DependencyObject) },
                       null
                    ).Invoke(null, new object[] { this });
                }
                return _itemsOwner;
            }
        }
        protected IVirtzPanelOwner VirtzPanelOwner
        {
            get
            {
                if (_virtzPanelOwner == null)
                    _virtzPanelOwner = ItemsOwner as IVirtzPanelOwner;
                return _virtzPanelOwner;
            }
        }
        protected new IRecyclingItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_itemContainerGenerator == null)
                {
                    /* Because of a bug in the framework the ItemContainerGenerator 
                     * is null until InternalChildren accessed at least one time. */
                    var children = InternalChildren;
                    _itemContainerGenerator = (IRecyclingItemContainerGenerator)base.ItemContainerGenerator;
                }
                return _itemContainerGenerator;
            }
        }
        public double ScrollHorizontalOffset
        {
            get { return (double)GetValue(ScrollHorizontalOffsetProperty); }
            set { SetValue(ScrollHorizontalOffsetProperty, value); }
        }
        public double ScrollVerticalOffset
        {
            get { return (double)GetValue(ScrollVerticalOffsetProperty); }
            set { SetValue(ScrollVerticalOffsetProperty, value); }
        }
        public double ScrollExtentWidth
        {
            get { return (double)GetValue(ScrollExtentWidthProperty); }
            set { SetValue(ScrollExtentWidthProperty, value); }
        }
        public double ScrollExtentHeight
        {
            get { return (double)GetValue(ScrollExtentHeightProperty); }
            set { SetValue(ScrollExtentHeightProperty, value); }
        }
        public static double GetScrollExtentWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(ScrollExtentWidthProperty);
        }
        public static void SetScrollExtentWidth(DependencyObject obj, double value)
        {
            obj.SetValue(ScrollExtentWidthProperty, value);
        }
        public static double GetScrollExtentHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(ScrollExtentHeightProperty);
        }
        public static void SetScrollExtentHeight(DependencyObject obj, double value)
        {
            obj.SetValue(ScrollExtentHeightProperty, value);
        }
        public static double GetScrollVerticalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(ScrollVerticalOffsetProperty);
        }
        public static void SetScrollVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(ScrollVerticalOffsetProperty, value);
        }
        public static double GetScrollHorizontalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(ScrollHorizontalOffsetProperty);
        }
        public static void SetScrollHorizontalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(ScrollHorizontalOffsetProperty, value);
        }
        #endregion

        #region IScrollInfo
        ScrollViewer IScrollInfo.ScrollOwner { get => _scrollOwner; set => _scrollOwner = value; }
        bool IScrollInfo.CanVerticallyScroll { get; set; }
        bool IScrollInfo.CanHorizontallyScroll { get; set; }
        double IScrollInfo.HorizontalOffset => ScrollHorizontalOffset;
        double IScrollInfo.VerticalOffset => ScrollVerticalOffset;
        double IScrollInfo.ExtentWidth => ScrollExtentWidth;
        double IScrollInfo.ExtentHeight => ScrollExtentHeight;
        double IScrollInfo.ViewportWidth => ScrollViewport.Width;
        double IScrollInfo.ViewportHeight => ScrollViewport.Height;
        void IScrollInfo.LineUp()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineUpScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.LineDown()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineDownScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.LineLeft()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineLeftScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        void IScrollInfo.LineRight()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineRightScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle) => OnMakeVisible(visual, rectangle);
        void IScrollInfo.MouseWheelUp()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelUpScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.MouseWheelDown()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelDownScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.MouseWheelLeft()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelLeftScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        void IScrollInfo.MouseWheelRight()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelRightScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        void IScrollInfo.PageUp()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -ScrollViewport.Height : GetPageUpScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.PageDown()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? ScrollViewport.Height : GetPageDownScrollAmount();
            ScrollVerticalOffset += amount;
        }
        void IScrollInfo.PageLeft()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? -ScrollViewport.Height : GetPageLeftScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        void IScrollInfo.PageRight()
        {
            var amount = ScrollUnit == ScrollUnit.Pixel ? ScrollViewport.Height : GetPageRightScrollAmount();
            ScrollHorizontalOffset += amount;
        }
        void IScrollInfo.SetHorizontalOffset(double offset) => ScrollHorizontalOffset = offset;
        void IScrollInfo.SetVerticalOffset(double offset) => ScrollVerticalOffset = offset;

        /// <summary>
        /// 内容视区
        /// </summary>
        protected Size ScrollViewport = new Size(0, 0);
        ///// <summary>
        ///// 滚动偏移量
        ///// </summary>
        //protected Point ScrollOffset = new Point(0, 0);

        protected virtual Size GetScrollSize() => new Size(10, 10);
        protected virtual Rect OnMakeVisible(Visual visual, Rect rectangle)
        {
            var offsetX = ScrollHorizontalOffset;
            var offsetY = ScrollVerticalOffset;

            Point pos = visual.TransformToAncestor(this).Transform(new Point(offsetX, offsetY));

            double scrollAmountX = 0;
            double scrollAmountY = 0;

            if (pos.X < offsetX)
            {
                scrollAmountX = -(offsetX - pos.X);
            }
            else if ((pos.X + rectangle.Width) > (offsetX + ScrollViewport.Width))
            {
                scrollAmountX = (pos.X + rectangle.Width) - (offsetX + ScrollViewport.Width);
            }

            if (pos.Y < offsetY)
            {
                scrollAmountY = -(offsetY - pos.Y);
            }
            else if ((pos.Y + rectangle.Height) > (offsetY + ScrollViewport.Height))
            {
                scrollAmountY = (pos.Y + rectangle.Height) - (offsetY + ScrollViewport.Height);
            }

            ScrollHorizontalOffset = offsetX + scrollAmountX;
            ScrollVerticalOffset = offsetY + scrollAmountY;

            double visibleRectWidth = Math.Min(rectangle.Width, ScrollViewport.Width);
            double visibleRectHeight = Math.Min(rectangle.Height, ScrollViewport.Height);

            return new Rect(scrollAmountX, scrollAmountY, visibleRectWidth, visibleRectHeight);
        }
        protected virtual double GetLineUpScrollAmount() => -this.GetScrollSize().Height;
        protected virtual double GetLineDownScrollAmount() => this.GetScrollSize().Height;
        protected virtual double GetLineLeftScrollAmount() => -this.GetScrollSize().Width;
        protected virtual double GetLineRightScrollAmount() => this.GetScrollSize().Width;
        protected virtual double GetMouseWheelUpScrollAmount() => -Math.Min(this.GetScrollSize().Height * MouseWheelDeltaItem, ScrollViewport.Height);
        protected virtual double GetMouseWheelDownScrollAmount() => Math.Min(this.GetScrollSize().Height * MouseWheelDeltaItem, ScrollViewport.Height);
        protected virtual double GetMouseWheelLeftScrollAmount() => -Math.Min(this.GetScrollSize().Width * MouseWheelDeltaItem, ScrollViewport.Width);
        protected virtual double GetMouseWheelRightScrollAmount() => Math.Min(this.GetScrollSize().Width * MouseWheelDeltaItem, ScrollViewport.Width);
        protected virtual double GetPageUpScrollAmount() => -ScrollViewport.Height;
        protected virtual double GetPageDownScrollAmount() => ScrollViewport.Height;
        protected virtual double GetPageLeftScrollAmount() => -ScrollViewport.Width;
        protected virtual double GetPageRightScrollAmount() => ScrollViewport.Width;
        #endregion

        #region IVirtzPanel
        IReadOnlyCollection<object> IVirtzPanel.Items => Items;
        Size IVirtzPanel.FirstChildDesiredSize => _firstChildDesiredSize;
        Size IVirtzPanel.GetFirstChildDesiredSize() => GetFirstChildSize();
        #endregion

        #region MeasureOverride
        protected override Size MeasureOverride(Size availableSize)
        {


            var groupItem = ItemsOwner as IHierarchicalVirtualizationAndScrollInfo;

            var extentWidth = ScrollExtentWidth;
            var extentHeight = ScrollExtentHeight;
            var offsetX = ScrollHorizontalOffset;
            var offsetY = ScrollVerticalOffset;

            Size desiredSize;

            if (groupItem != null)
            {
                /* If the ItemsOwner is a group item the availableSize is ifinity. 
                 * Therfore the vieport size provided by the group item is used. */
                var viewportSize = groupItem.Constraints.Viewport.Size;
                var headerSize = groupItem.HeaderDesiredSizes.PixelSize;
                double availableWidth = Math.Max(viewportSize.Width - 5, 0); // left margin of 5 dp
                double availableHeight = Math.Max(viewportSize.Height - headerSize.Height, 0);
                availableSize = new Size(availableWidth, availableHeight);
                desiredSize = new Size(extentWidth, extentHeight);
            }
            else
            {
                double desiredWidth = Math.Min(availableSize.Width, extentWidth);
                double desiredHeight = Math.Min(availableSize.Height, extentHeight);
                desiredSize = new Size(desiredWidth, desiredHeight);
            }

            if (groupItem != null)
            {
                var scrollOffset = groupItem.Constraints.Viewport.Location;
                ScrollHorizontalOffset = scrollOffset.X;
                ScrollVerticalOffset = scrollOffset.Y;

                ScrollViewport = groupItem.Constraints.Viewport.Size;
                CacheLength = groupItem.Constraints.CacheLength;
                CacheLengthUnit = groupItem.Constraints.CacheLengthUnit; // can be Item or Pixel
            }
            else
            {
                if (ScrollViewport.Height != 0 && offsetY != 0 && offsetY + ScrollViewport.Height + 1 >= ScrollExtentHeight)
                    ScrollVerticalOffset = extentHeight - availableSize.Height;
                if (ScrollViewport.Width != 0 && offsetX != 0 && offsetX + ScrollViewport.Width + 1 >= ScrollExtentWidth)
                    ScrollHorizontalOffset = extentWidth - availableSize.Width;
                if (availableSize != ScrollViewport)
                {
                    ScrollViewport = availableSize;
                    _scrollOwner?.InvalidateScrollInfo();
                }

                CacheLength = GetCacheLength(ItemsOwner);
                CacheLengthUnit = GetCacheLengthUnit(ItemsOwner); // can be Page, Item or Pixel
            }

            ItemRangeCollection itemRanges = new ItemRangeCollection();
            var owner = VirtzPanelOwner;
            if (owner != null)
            {
                _firstChildDesiredSize = GetFirstChildSize();
                owner.Measure(this, availableSize);

                int index = -1, startIndex = -1, endIndex = -1;
                foreach (var item in Items)
                {
                    index++;

                    if (owner.CanArrangeItem(this, item))
                    {
                        if (startIndex == -1)
                            startIndex = index;
                        endIndex = index;
                    }
                    else
                    {
                        if (startIndex >= 0 && endIndex >= 0)
                        {
                            itemRanges.Add(startIndex, endIndex);
                        }

                        startIndex = -1;
                        endIndex = -1;
                    }
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    itemRanges.Add(startIndex, endIndex);
                }
            }

            RealizeChildren(itemRanges);
            VirtualizeChildren(itemRanges);

            return desiredSize;
        }
        /// <summary>
        /// 实例化子元素
        /// </summary>
        protected virtual void RealizeChildren(ItemRangeCollection itemRanges)
        {
            if (itemRanges == null)
                return;
            foreach (var itemRange in itemRanges)
            {
                var startPosition = ItemContainerGenerator.GeneratorPositionFromIndex(itemRange.StartIndex);

                int childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;

                using (ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
                {
                    foreach (var item in itemRange.GetEnumerator())
                    {
                        UIElement child = (UIElement)ItemContainerGenerator.GenerateNext(out bool isNewlyRealized);
                        if (isNewlyRealized || /*recycled*/!InternalChildren.Contains(child))
                        {
                            if (childIndex >= InternalChildren.Count)
                            {
                                AddInternalChild(child);
                            }
                            else
                            {
                                InsertInternalChild(childIndex, child);
                            }
                            ItemContainerGenerator.PrepareItemContainer(child);

                            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        }

                        if (child is IHierarchicalVirtualizationAndScrollInfo groupItem)
                        {
                            groupItem.Constraints = new HierarchicalVirtualizationConstraints(
                                new VirtualizationCacheLength(0),
                                VirtualizationCacheLengthUnit.Item,
                                new Rect(0, 0, ScrollViewport.Width, ScrollViewport.Height));
                            child.Measure(new Size(ScrollViewport.Width, ScrollViewport.Height));
                        }

                        childIndex++;
                    }
                }
            }
        }
        /// <summary>
        /// 虚拟化子元素
        /// </summary>
        protected virtual void VirtualizeChildren(ItemRangeCollection itemRanges)
        {
            for (int childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
            {
                var generatorPosition = GetGeneratorPositionFromChildIndex(childIndex);

                int itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(generatorPosition);

                if (itemRanges.Contains(itemIndex) == false)
                {

                    if (VirtualizationMode == VirtualizationMode.Recycling)
                    {
                        ItemContainerGenerator.Recycle(generatorPosition, 1);
                    }
                    else
                    {
                        ItemContainerGenerator.Remove(generatorPosition, 1);
                    }
                    RemoveInternalChildRange(childIndex, 1);
                }
            }
        }
        protected Size GetFirstChildSize()
        {
            if (InternalChildren.Count != 0)
                return InternalChildren[0].DesiredSize;
            var startPosition = ItemContainerGenerator.GeneratorPositionFromIndex(0);
            using (ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
            {
                var child = (UIElement)ItemContainerGenerator.GenerateNext();
                if (child != null)
                {
                    AddInternalChild(child);
                    ItemContainerGenerator.PrepareItemContainer(child);
                    child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return child.DesiredSize;
                }
            }

            return new Size();
        }
        protected int GetItemIndexFromChildIndex(int childIndex)
        {
            var generatorPosition = GetGeneratorPositionFromChildIndex(childIndex);
            return ItemContainerGenerator.IndexFromGeneratorPosition(generatorPosition);
        }
        protected virtual GeneratorPosition GetGeneratorPositionFromChildIndex(int childIndex)
        {
            return new GeneratorPosition(childIndex, 0);
        }
        #endregion

        #region ArrangeOverride
        protected override Size ArrangeOverride(Size finalSize)
        {
            for (int childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                UIElement child = InternalChildren[childIndex];
                var itemIndex = GetItemIndexFromChildIndex(childIndex);
                var item = Items[itemIndex];
                var rect = VirtzPanelOwner?.ArrangeItem(this, child, item) ?? new Rect();
                child.Arrange(rect);
            }
            return finalSize;
        }
        #endregion

        #region Items
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }
        }
        #endregion
    }
    public struct ItemRange
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public int Count { get; }
        public ItemRange(int index) : this(index, index) { }
        public ItemRange(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < startIndex) throw new ArgumentException();
            StartIndex = startIndex;
            EndIndex = endIndex;
            Count = endIndex - startIndex + 1;
        }
        public IEnumerable<int> GetEnumerator()
        {
            if (StartIndex < 0 || EndIndex < StartIndex) yield break;
            for (int i = StartIndex; i < EndIndex + 1; i++)
            {
                yield return i;
            }
        }
        public bool Contains(int itemIndex)
        {
            if (StartIndex < 0 || EndIndex < StartIndex)
                return false;
            return itemIndex >= StartIndex && itemIndex <= EndIndex;
        }
        public override string ToString() => $"{StartIndex}-{EndIndex}";
    }
    public class ItemRangeCollection : IEnumerable<ItemRange>
    {
        private LinkedList<ItemRange> _ranges;
        public ItemRangeCollection()
        {
            _ranges = new LinkedList<ItemRange>();
        }
        public void Add(ItemRange range)
        {
            if (range.Count == 0) throw new ArgumentException();
            var last = _ranges.Last;
            if (last != null)
            {
                var lastRange = last.Value;
                if (range.StartIndex < lastRange.EndIndex) throw new ArgumentException();
            }
            _ranges.AddLast(range);
        }
        public int Count
        {
            get
            {
                var count = 0;
                foreach (var item in this)
                {
                    count += item.Count;
                }
                return count;
            }
        }
        public void Add(int startIndex, int endIndex) => this.Add(new ItemRange(startIndex, endIndex));
        public void Add(int index) => this.Add(new ItemRange(index));
        public bool Contains(int index)
        {
            foreach (var item in this)
            {
                if (item.Contains(index))
                {
                    return true;
                }
            }
            return false;
        }
        public IEnumerator<ItemRange> GetEnumerator()
        {
            return _ranges.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        public override string ToString() => $"Count:{Count}  {string.Join("|", _ranges)}";
    }
    public interface IVirtzPanelOwner
    {
        /// <summary>
        /// 测量
        /// </summary>
        /// <param name="virtzPanel"></param>
        /// <param name="availableSize"></param>
        void Measure(IVirtzPanel virtzPanel, Size availableSize);
        /// <summary>
        /// 是否设置元素布局
        /// </summary>
        /// <param name="virtzPanel"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        bool CanArrangeItem(IVirtzPanel virtzPanel, object item);
        /// <summary>
        /// 设置元素布局
        /// </summary>
        /// <param name="virtzPanel"></param>
        /// <param name="containter">元素容器</param>
        /// <param name="item"></param>
        /// <returns></returns>
        Rect ArrangeItem(IVirtzPanel virtzPanel, UIElement containter, object item);
    }
    public interface IVirtzPanel : IScrollInfo
    {
        IReadOnlyCollection<object> Items { get; }
        Size FirstChildDesiredSize { get; }
        /// <summary>
        /// 计算第一个元素的宽度(无元素下默认0,0)
        /// </summary>
        Size GetFirstChildDesiredSize();
    }
}
