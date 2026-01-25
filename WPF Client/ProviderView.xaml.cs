using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    public ProviderView(ProviderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.MessageItems.CollectionChanged += MessageItems_CollectionChanged;
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
}
