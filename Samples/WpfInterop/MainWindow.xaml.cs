using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using WindowsInput = System.Windows.Input;
using WindowsMedia = System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Xna.Framework.Graphics;
using nkast.ProtonType.XnaGraphics;
using nkast.ProtonType.XnaGraphics.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using XnaInput = Microsoft.Xna.Framework.Input;

namespace WpfInterop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Stopwatch _stopwatch;
        private GameServiceContainer _services;
        private IGraphicsDeviceService _graphicsDeviceService;
        private BaseGraphicsDeviceManager _graphicsDeviceManager0;
        private BaseGraphicsDeviceManager _graphicsDeviceManager1;
        private ContentManager _content;
        SpriteBatch _sp;

        Texture2D _txBox;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle;
            _graphicsDeviceService = GraphicsDeviceService.AddRef(handle, 1, 1);

            _services = new GameServiceContainer();
            _services.AddService(typeof(IGraphicsDeviceService), _graphicsDeviceService);

            _content = new ContentManager(_services);
            LoadContent();

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            WindowsMedia.CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
        }

        private void LoadContent()
        {
            IGraphicsDeviceService graphicsDeviceService = (IGraphicsDeviceService)_services.GetService(typeof(IGraphicsDeviceService));
            System.Diagnostics.Debug.Assert(graphicsDeviceService == _graphicsDeviceService);

            GraphicsDevice gd = graphicsDeviceService.GraphicsDevice;

            _sp = new SpriteBatch(gd);
            _txBox = new Texture2D(gd, 2, 2);
            _txBox.SetData(new[]
            {
                Color.Red, Color.Green,
                Color.Blue, Color.White,
            } );
        }

        private void xnaImage0_Loaded(object sender, RoutedEventArgs e)
        {
            _graphicsDeviceManager0 = new D3DImageGraphicsDeviceManager(xnaImage0);
            xnaImage0.SizeChanged += XnaImage0_SizeChanged;
            xnaImage0.MouseDown += XnaImage0_MouseDown;
        }

        private void xnaImage1_Loaded(object sender, RoutedEventArgs e)
        {
            _graphicsDeviceManager1 = new D3DImageGraphicsDeviceManager(xnaImage1);
            xnaImage1.SizeChanged += XnaImage1_SizeChanged;
            xnaImage1.MouseDown += XnaImage1_MouseDown;    
            xnaImage_Paint(_graphicsDeviceManager1);
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (XnaInput.Mouse.WindowHandle != IntPtr.Zero)
            {
                XnaInput.MouseState mouseState = XnaInput.Mouse.GetState();
                this.Title = String.Format("MouseState pos: [{0},{1}], wheel:[{2}], LButton: [{3}], Raw: [{4},{5}]",
                    mouseState.X, mouseState.Y, mouseState.ScrollWheelValue, mouseState.LeftButton,
                    mouseState.RawX, mouseState.RawY);
            }

            // update Image 1 on every Composition frame.
            xnaImage_Paint(_graphicsDeviceManager0);
        }

        private void XnaImage0_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // update Image 0 on SizeChanged.
            xnaImage_Paint(_graphicsDeviceManager0);
        }

        private void XnaImage0_MouseDown(object sender, WindowsInput.MouseButtonEventArgs e)
        {
            xnaImage0.Focus(); // set Mouse.WindowHandle
        }

        private void XnaImage1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // update Image 1 on SizeChanged.
            xnaImage_Paint(_graphicsDeviceManager1);
        }

        private void XnaImage1_MouseDown(object sender, WindowsInput.MouseButtonEventArgs e)
        {
            xnaImage1.Focus(); // set Mouse.WindowHandle

            // update Image 1 on user interaction.
            xnaImage_Paint(_graphicsDeviceManager1);
        }

        private void xnaImage_Paint(BaseGraphicsDeviceManager graphicsDeviceManager)
        {
            bool shouldDrawFrame = false;
            try
            {
                shouldDrawFrame = graphicsDeviceManager.BeginDraw();
                if (shouldDrawFrame)
                {
                    IGraphicsDeviceService graphicsDeviceService = (IGraphicsDeviceService)_services.GetService(typeof(IGraphicsDeviceService));
                    Debug.Assert(graphicsDeviceService == _graphicsDeviceService);

                    DrawRotatingBox(graphicsDeviceService.GraphicsDevice, graphicsDeviceManager.DefaultRenderTarget, _stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                if (shouldDrawFrame)
                {
                    graphicsDeviceManager.EndDraw();
                }
            }
        }

        private void DrawRotatingBox(GraphicsDevice gd, RenderTarget2D defaultRenderTarget, TimeSpan elapsed)
        {
            gd.SetRenderTarget(defaultRenderTarget);
            gd.Clear(Color.CornflowerBlue);
            
            Vector2 position = new Vector2(gd.Viewport.Width, gd.Viewport.Height) /2f;
            Vector2 origin = new Vector2(_txBox.Width, _txBox.Height) /2f;
            float scale = (gd.Viewport.Height / _txBox.Width) * gd.Viewport.AspectRatio;

            float q = (float)(Math.PI * 2 * elapsed.TotalSeconds);

            _sp.Begin();
            _sp.Draw(_txBox, position, null, Color.White, q, origin, scale, SpriteEffects.None, 0);
            _sp.End();

        }
    }
}
