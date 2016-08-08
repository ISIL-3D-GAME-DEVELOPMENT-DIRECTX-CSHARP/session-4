using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Device = SharpDX.Direct3D11.Device;

namespace Sesion2_Lab01 {
    public class NativeApplication {

        private const int App_Width = 800;
        private const int App_Height = 600;

        // Es como el Windows.Forms, pero este se utiliza nativamente para el DirectX
        // Se necesita las librerias:
        //  1. System.Windows.Forms
        //  2. SharpDX.Windows
        private RenderForm mRenderForm;

        private Texture2D mBackBufferFBO;
        private RenderTargetView mRenderTargetView;
        private Device mDevice;
        private DeviceContext mDeviceContext;

        private Factory mFactory;

        private RenderCamera mRenderCamera;
        private ShaderTextureProgram mShaderProgram;

        private NTexture2D mTexture2D;

        private SwapChain mSwapChain;
        private SwapChainDescription mSwapChainDescription;

        // variables para dibujar nuestro primitivo
        private ushort[] mIndices;
        private float[] mVertices;

        public NativeApplication() {
            mRenderForm = new RenderForm("Sesion 4 :: Implementacion de Texturas");

            mRenderForm.KeyPress += mRenderForm_KeyPress;

            mSwapChainDescription = new SwapChainDescription(); // es una estructura, no es necesario construirlo
            mSwapChainDescription.BufferCount = 1;
            mSwapChainDescription.IsWindowed = true; // es ventana
            mSwapChainDescription.SwapEffect = SwapEffect.Discard;
            mSwapChainDescription.Usage = Usage.RenderTargetOutput;
            mSwapChainDescription.OutputHandle = mRenderForm.Handle; // le pasamos el puntero de nuestro RenderForm
            mSwapChainDescription.SampleDescription.Count = 1;
            mSwapChainDescription.SampleDescription.Quality = 0;
            mSwapChainDescription.ModeDescription.Width = NativeApplication.App_Width;  // aqui definimos el ancho de la ventana
            mSwapChainDescription.ModeDescription.Height = NativeApplication.App_Height; // aqui definimos el alto de la ventana
            mSwapChainDescription.ModeDescription.RefreshRate.Numerator = 60; // aqui definimos el Frame Rate
            mSwapChainDescription.ModeDescription.RefreshRate.Denominator = 1; // siempre por defecto 1... la matematica es Denominator / Numerator
            mSwapChainDescription.ModeDescription.Format = Format.R8G8B8A8_UNorm; // se define el formato del color de la ventana

            // Creamos la tarjeta grafica usando el SwapChainDescription
            // Esto nos devuelve:
            //  1. Device -> La tarjeta grafica
            //  2. SwapChain -> Control del Frame Rate
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, mSwapChainDescription,
                out mDevice, out mSwapChain);

            // recogemos el contexto de la tarjeta de video
            mDeviceContext = mDevice.ImmediateContext;

            // recogemos el padre constructor de la ventana
            mFactory = mSwapChain.GetParent<Factory>();
            // ignoramos todos los eventos de la ventana
            mFactory.MakeWindowAssociation(mRenderForm.Handle, WindowAssociationFlags.IgnoreAll);

            // creamos un frame buffer object, y usamos este back buffer para almacenar la data del render
            mBackBufferFBO = Texture2D.FromSwapChain<Texture2D>(mSwapChain, 0);

            // creamos un render target view para manejar el render usando nuestro frame buffer object
            mRenderTargetView = new RenderTargetView(mDevice, mBackBufferFBO);

            // cargamos nuestra textura
            mTexture2D = new NTexture2D(mDevice);
            mTexture2D.Load("Content/spMario.png");

            // cargamos y construimos nuestro Shader
            mShaderProgram = new ShaderTextureProgram(mDevice);
            mShaderProgram.Load("Content/Fx_TexturePrimitive.fx");

            mShaderProgram.X = 100;
            mShaderProgram.Y = 100;

            // Ahora inicializamos algunos datos para poder dibujar
            PreConfiguration();

            // Ahora creamos algo muy importante! Nuestro Render Loop, donde ira nuestro Draw y Update!
            RenderLoop.Run(mRenderForm, OnRenderLoop);
        }

        private void mRenderForm_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
            switch(e.KeyChar) {
            case 'w':
                mShaderProgram.Y -= 3;
                break;
            case 's':
                mShaderProgram.Y += 3;
                break;
            }
        }

        private void PreConfiguration() {
            // preparamos nuestros parametros para dibujar
            // creamos una camara para el escenario
            mRenderCamera = new RenderCamera(NativeApplication.App_Width, NativeApplication.App_Height);

            // Creamos nuestro Viewport que define el tamanio de nuestras dimension de dibujo para
            // el DirectX
            mDeviceContext.Rasterizer.SetViewports(new Viewport(0, 0, NativeApplication.App_Width,
                NativeApplication.App_Height, 0.0f, 1.0f));

            // ahora pasamos nuestro Frame Buffer Object
            mDeviceContext.OutputMerger.SetTargets(mRenderTargetView);

            // creamos nuestro indices
            mIndices = new ushort[6];
            mIndices[0] = 0;
            mIndices[1] = 1;
            mIndices[2] = 2;
            mIndices[3] = 0;
            mIndices[4] = 2;
            mIndices[5] = 3;

            // creamos nustros vertices
            mVertices = new float[10 * 4];
            // nuestro primer vertice
            mVertices[0] = 0f; mVertices[1] = 0f; mVertices[2] = 0f; mVertices[3] = 1f; // vertex
            mVertices[4] = 1f; mVertices[5] = 1f; mVertices[6] = 1f; mVertices[7] = 1f; // color
            mVertices[8] = 0f; mVertices[9] = 0f; // texture coordinate
            // nuestro segundo vertice
            mVertices[10] = 150f; mVertices[11] = 0f; mVertices[12] = 0f; mVertices[13] = 1f; // vertex
            mVertices[14] = 1f; mVertices[15] = 1f; mVertices[16] = 1f; mVertices[17] = 1f; // color
            mVertices[18] = 1f; mVertices[19] = 0f; // texture coordinate
            // nuestro tercer vertice
            mVertices[20] = 150f; mVertices[21] = 150f; mVertices[22] = 0f; mVertices[23] = 1f; // vertex
            mVertices[24] = 1f; mVertices[25] = 1f; mVertices[26] = 1f; mVertices[27] = 1f; // color
            mVertices[28] = 1f; mVertices[29] = 1f; // texture coordinate
            // nuestro cuarto vertice
            mVertices[30] = 0f; mVertices[31] = 150f; mVertices[32] = 0f; mVertices[33] = 1f; // vertex
            mVertices[34] = 1f; mVertices[35] = 1f; mVertices[36] = 1f; mVertices[37] = 1f; // color
            mVertices[38] = 0f; mVertices[39] = 1f; // texture coordinate
        }

        private void OnRenderLoop() {
            Update();
            Draw();
        }

        private void Update() {
            // actualizamos nuestra camara
            mRenderCamera.Update();

            // mandamos la informacion al Shader
            mShaderProgram.Update(mVertices, mIndices);
        }

        private void Draw() {
            // aqui definimos nuestro color usando Color4
            Color4 clearColor = Color4.Black;
            clearColor.Red = 0f;
            clearColor.Green = 1f;
            clearColor.Blue = 0f;

            // aqui limpiamos nuestra ventana con un color solido
            mDeviceContext.ClearRenderTargetView(mRenderTargetView, clearColor);

            /////////////////// EMPEZAMOS A DIBUJAR NUESTRO PRIMITIVO ///////////////
            mShaderProgram.Draw(mRenderCamera.transformed, mTexture2D);

            // hacemos un swap de buffers
            mSwapChain.Present(0, PresentFlags.None);
        }
    }
}
