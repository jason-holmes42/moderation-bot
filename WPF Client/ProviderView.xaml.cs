using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFClient;
/// <summary>
/// Interaction logic for ProviderView.xaml
/// </summary>
public partial class ProviderView : UserControl
{
    public event Action<ProviderView>? RequestClose;

    private readonly double minWinHeight = 250;
    private readonly double minWinWidth = 500;

    public ProviderView(ProviderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.MessageItems.CollectionChanged += MessageItems_CollectionChanged;

        ResizeRight.DragDelta += ResizeThumb_DragDelta;
        ResizeLeft.DragDelta += ResizeThumb_DragDelta;
        ResizeTop.DragDelta += ResizeThumb_DragDelta;
        ResizeBottom.DragDelta += ResizeThumb_DragDelta;
        ResizeCornerBR.DragDelta += ResizeThumb_DragDelta;
        ResizeCornerBL.DragDelta += ResizeThumb_DragDelta;
        ResizeCornerTR.DragDelta += ResizeThumb_DragDelta;
        ResizeCornerTL.DragDelta += ResizeThumb_DragDelta;
    }

    // Chat input functions
    private void ChatInput_Send(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderViewModel viewModel)
        {
            viewModel.SendMessage(txtChatInput.Text);
            txtChatInput.Text = string.Empty;
            txtChatInput.Focus();
        }
    }
    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ChatInput_Send(sender, e);
        }
    }
    private void ChatInput_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null && textBox.Text == LocalizationManager.Instance["ChatInputPlaceholder"])
        {
            textBox.Text = string.Empty;
            textBox.Foreground = new SolidColorBrush(Colors.Black);
        }
    }
    private void ChatInput_LostFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null && string.IsNullOrEmpty(textBox.Text))
        {
            textBox.Text = LocalizationManager.Instance["ChatInputPlaceholder"];
            textBox.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }
    private void ChatInput_Loaded(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        textBox.Text = LocalizationManager.Instance["ChatInputPlaceholder"];
    }

    // X to close functionality
    private void btnClose(object sender,  RoutedEventArgs e)
    {
        RequestClose?.Invoke(this);
    }
    private void ProviderView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }

    // Enable click-to-drag movement on TitleBar element
    private Point _mouseMoveStart;
    private double _elementStartLeft;
    private double _elementStartTop;
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _mouseMoveStart = e.GetPosition(Parent as UIElement);
        _elementStartLeft = Canvas.GetLeft(this);
        _elementStartTop = Canvas.GetTop(this);
        Mouse.Capture(TitleBar);
    }
    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (Mouse.Captured != TitleBar) return;

        Canvas canvas = Parent as Canvas;
        Point pos = e.GetPosition(canvas);

        double deltaX = pos.X - _mouseMoveStart.X;
        double deltaY = pos.Y - _mouseMoveStart.Y;

        Canvas.SetLeft(this, _elementStartLeft + deltaX);
        Canvas.SetTop(this, _elementStartTop + deltaY);
    }
    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
    }

    // If new entries are multi-line, the scroll viewer can't keep up and will fall behind, so this forces it to adjust accordingly.
    private void MessageItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        bool isAtBottom = sclMessages.VerticalOffset == sclMessages.ExtentHeight;

        // If not already scrolled to the bottom, do nothing so as to not disrupt backreading
        if (!isAtBottom) return;

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            // Ask the dispatcher to scroll to the end after loading is complete so that the calculation is done when it tries to scroll
            sclMessages.Dispatcher.BeginInvoke(new Action(() =>
            {
                sclMessages.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    // Control resize functionality
    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Thumb thumb = (Thumb)sender;

        // Identify in which direction(s) to change the window
        int horizontalSign = 0;
        int verticalSign = 0;

        // Alignment defaults to 'Stretch' if not set, so we can use it to identify which side of the window the controls are on.
        if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
        {
            // The math of control resizing assumes a right/bottom bias, meaning left and top controls need to invert their math to resize correctly.
            horizontalSign = -1;
        }
        else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)
        {
            horizontalSign = 1;
        }
        // Else would mean alignment = Stretch, which would be either top or bottom. Since those should have their horizontal axis locked, we don't do anything with the width.

        if (thumb.VerticalAlignment == VerticalAlignment.Top)
        {
            verticalSign = -1;
        }
        else if (thumb.VerticalAlignment== VerticalAlignment.Bottom)
        {
            verticalSign = 1;
        }

        // Apply any changes accordingly
        double newWidth = Width + (e.HorizontalChange * horizontalSign);
        double newHeight = Height + (e.VerticalChange * verticalSign);

        if (newWidth > minWinWidth)
        {
            this.Width = newWidth;
            // Since location is also based on the top left corner of a control, you also need to move the control while resizing top or left sides to get the proper effect.
            if (horizontalSign == -1) Canvas.SetLeft(this, Canvas.GetLeft(this) + e.HorizontalChange);
        }
        if (newHeight > minWinHeight)
        {
            this.Height = newHeight;
            if (verticalSign == -1)Canvas.SetTop(this, Canvas.GetTop(this) + e.VerticalChange);
        }
    }
}
