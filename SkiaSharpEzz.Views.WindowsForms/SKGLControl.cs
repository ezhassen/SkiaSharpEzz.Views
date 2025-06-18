using OpenTK.GLControl;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SkiaSharpEzz.Views.Desktop
{
    [DefaultEvent("PaintSurface")]
    [DefaultProperty("Name")]
    public class SKGLControlEzz : GLControl
    {
        private const SKColorType colorType = SKColorType.Rgba8888;
        private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

        private bool designMode;

        private GRContext grContext;
        private GRGlFramebufferInfo glInfo;
        private GRBackendRenderTarget renderTarget;
        private SKSurface surface;
        private SKCanvas canvas;

        private SKSizeI lastSize;

        public SKGLControlEzz(SupportedGLBindings selectedGLBindings = SupportedGLBindings.ES20) : base()
        {
            Initialize();
            SelectedGLBindings = selectedGLBindings;
        }

        public SKGLControlEzz(GLControlSettings gLControlSettings, SupportedGLBindings selectedGLBindings = SupportedGLBindings.ES20) : base(gLControlSettings)
        {
            Initialize();
            SelectedGLBindings = selectedGLBindings;
        }

        //public SKGLControl()
        //	: base(new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24, 8))
        //{
        //	Initialize();
        //}

        //public SKGLControl(GraphicsMode mode)
        //	: base(mode)
        //{
        //	Initialize();
        //}

        //public SKGLControl(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
        //	: base(mode, major, minor, flags)
        //{
        //	Initialize();
        //}

        private void Initialize()
        {
            designMode = DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;

            ResizeRedraw = true;
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public SupportedGLBindings SelectedGLBindings { get; set; }

        public SKSize CanvasSize => lastSize;

        public GRContext GRContext => grContext;

        [Category("Appearance")]
        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurface;

        protected override void OnPaint(PaintEventArgs e)
        {
            if (designMode)
            {
                e.Graphics.Clear(BackColor);
                return;
            }

            base.OnPaint(e);

            MakeCurrent();

            // create the contexts if not done already
            if (grContext == null)
            {
                var glInterface = GRGlInterface.Create();
                grContext = GRContext.CreateGl(glInterface);
            }

            // get the new surface size
            var newSize = new SKSizeI(Width, Height);

            // manage the drawing surface
            if (renderTarget == null || lastSize != newSize || !renderTarget.IsValid)
            {
                // create or update the dimensions
                lastSize = newSize;

                //GL.GetInteger(GetPName.FramebufferBinding, out var framebuffer);
                //GL.GetInteger(GetPName.StencilBits, out var stencil);
                //GL.GetInteger(GetPName.Samples, out var samples);
                int framebuffer, stencil, samples;

                switch (this.SelectedGLBindings)
                {
                    case SupportedGLBindings.ES30:
                        OpenTK.Graphics.ES30.GL.GetInteger(OpenTK.Graphics.ES30.GetPName.FramebufferBinding, out framebuffer);
                        OpenTK.Graphics.ES30.GL.GetInteger(OpenTK.Graphics.ES30.GetPName.StencilBits, out stencil);
                        OpenTK.Graphics.ES30.GL.GetInteger(OpenTK.Graphics.ES30.GetPName.Samples, out samples);
                        break;
                    case SupportedGLBindings.OpenGL:
                        OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.FramebufferBinding, out framebuffer);
                        OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.StencilBits, out stencil);
                        OpenTK.Graphics.OpenGL.GL.GetInteger(OpenTK.Graphics.OpenGL.GetPName.Samples, out samples);
                        break;
                    //case SupportedGLBindings.ES20:
                    default:

                        OpenTK.Graphics.ES20.GL.GetInteger(OpenTK.Graphics.ES20.GetPName.FramebufferBinding, out framebuffer);
                        OpenTK.Graphics.ES20.GL.GetInteger(OpenTK.Graphics.ES20.GetPName.StencilBits, out stencil);
                        OpenTK.Graphics.ES20.GL.GetInteger(OpenTK.Graphics.ES20.GetPName.Samples, out samples);
                        break;
                }


                var maxSamples = grContext.GetMaxSurfaceSampleCount(colorType);
                if (samples > maxSamples)
                    samples = maxSamples;
                glInfo = new GRGlFramebufferInfo((uint)framebuffer, colorType.ToGlSizedFormat());

                // destroy the old surface
                surface?.Dispose();
                surface = null;
                canvas = null;

                // re-create the render target
                renderTarget?.Dispose();
                renderTarget = new GRBackendRenderTarget(newSize.Width, newSize.Height, samples, stencil, glInfo);
            }

            // create the surface
            if (surface == null)
            {
                surface = SKSurface.Create(grContext, renderTarget, surfaceOrigin, colorType);
                canvas = surface.Canvas;
            }

            using (new SKAutoCanvasRestore(canvas, true))
            {
                // start drawing
#pragma warning disable CS0612 // Type or member is obsolete
                OnPaintSurface(new SKPaintGLSurfaceEventArgs(surface, renderTarget, surfaceOrigin, colorType));
                //OnPaintSurface(new SKPaintGLSurfaceEventArgs(surface, renderTarget, surfaceOrigin, colorType, glInfo));
#pragma warning restore CS0612 // Type or member is obsolete
            }

            // update the control
            canvas.Flush();
            SwapBuffers();
        }



        protected virtual void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            // invoke the event
            PaintSurface?.Invoke(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // clean up
            canvas = null;
            surface?.Dispose();
            surface = null;
            renderTarget?.Dispose();
            renderTarget = null;
            grContext?.Dispose();
            grContext = null;
        }
    }
}
