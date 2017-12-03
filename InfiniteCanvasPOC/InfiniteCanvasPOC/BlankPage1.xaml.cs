using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace InfiniteCanvasPOC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage1 : Page
    {
        public BlankPage1()
        {
            this.InitializeComponent();

        //    inkCanvas.InkPresenter.InputDeviceTypes =
        //CoreInputDeviceTypes.Mouse |
        //CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

        //    // Set initial ink stroke attributes.
        //    InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
        //    drawingAttributes.Color = Windows.UI.Colors.Black;
        //    drawingAttributes.IgnorePressure = false;
        //    drawingAttributes.FitToCurve = true;
        //    inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

        //    inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
        }

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            
        }
    }
}
