using System;
using System.Collections.Generic;
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
    public ProviderView(ProviderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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
}
