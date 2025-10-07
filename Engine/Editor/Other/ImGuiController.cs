using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

public unsafe class ImGuiController
{
    public ImGuiContextPtr guiContext;
    public ImPlotContextPtr plotContext;

    private GL opengl;
    private IWindow window;
    private IInputContext input;
    private IKeyboard keyboard;

    private List<char> pressedchars = [];

    private int alocTex;
    private int alocProj;
    private int alocPos;
    private int alocUv;
    private int alocColor;

    private uint vao;
    private uint vbo;
    private uint ebo;
    
    private ImGuiShader shader;

    public ImGuiController(GL opengl, IWindow window, IInputContext input, string font = null, int fontSize = 14)
    {
        // set silk refs
        this.opengl = opengl;
        this.window = window;
        this.input = input;

        // create gui contexts and link them together
        guiContext = ImGui.CreateContext();
        plotContext = ImPlot.CreateContext();
        ImGui.SetCurrentContext(guiContext);
        ImGuizmo.SetImGuiContext(guiContext);
        ImPlot.SetImGuiContext(guiContext);
        ImPlot.SetCurrentContext(plotContext);

        // set initial display size
        ImGui.GetIO().DisplaySize = (Vector2)window.Size;
        ImGui.GetIO().DisplayFramebufferScale = new Vector2(window.FramebufferSize.X / window.Size.X, window.FramebufferSize.Y / window.Size.Y);
        
        // handle window resizing
        window.Resize += (newSize) => ImGui.GetIO().DisplaySize = (Vector2)newSize;

        // remember pressed silk keyboard chars
        keyboard = input.Keyboards[0];
        keyboard.KeyChar += (keyboard, character) => pressedchars.Add(character);

        // set flags
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures;
        ImGui.GetIO().Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
        ImGui.GetIO().ConfigFlags = ImGuiConfigFlags.DockingEnable;

        // creates shader and buffers used for imgui rendering
        SetupShaderAndBuffers();

        // custom style
        if (font != null)
        {
            ImGui.GetIO().Fonts.Clear();
            ImGui.GetIO().Fonts.AddFontFromFileTTF(font, fontSize);
        }
        ImGui.GetIO().Handle->IniFilename = null;
        ImGui.StyleColorsDark();
        SetupCustomTheme();
    }

    public void Update(float deltaTime)
    {
        ImGui.GetIO().DeltaTime = deltaTime;
        UpdateImGuiInput();
        ImGui.NewFrame();
        ImGuizmo.BeginFrame();
    }

    public void Render()
    {
        ImGui.Render();
        ImGui.EndFrame();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();

        var mouseState = input.Mice[0].CaptureState();
        var keyboardState = input.Keyboards[0].CaptureState();

        io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);
        io.MousePos = mouseState.Position;

        var wheel = mouseState.GetScrollWheels()[0];
        io.MouseWheel = wheel.Y;
        io.MouseWheelH = wheel.X;

        foreach (var key in Enum.GetValues<Key>())
        {
            if (key == Key.Unknown) continue;
            io.AddKeyEvent(ToImGuiKey(key), keyboardState.IsKeyPressed(key));
        }

        foreach (var pressed in pressedchars) io.AddInputCharacter(pressed);
        pressedchars.Clear();

        io.KeyCtrl = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
        io.KeyAlt = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);
        io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
        io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft) || keyboardState.IsKeyPressed(Key.SuperRight);
    }

    private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr)
    {
        opengl.Enable(GLEnum.Blend);
        opengl.BlendEquation(GLEnum.FuncAdd);
        opengl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
        opengl.Disable(GLEnum.CullFace);
        opengl.Disable(GLEnum.DepthTest);
        opengl.Disable(GLEnum.StencilTest);
        opengl.Enable(GLEnum.ScissorTest);

        float L = drawDataPtr.DisplayPos.X;
        float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
        float T = drawDataPtr.DisplayPos.Y;
        float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

        Span<float> orthoProjection = 
        [
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
        ];

        shader.UseShader();
        opengl.Uniform1(alocTex, 0);
        opengl.UniformMatrix4(alocProj, 1, false, orthoProjection);
        opengl.BindSampler(0, 0);
        
        vao = opengl.GenVertexArray();
        opengl.BindVertexArray(vao);

        opengl.BindBuffer(GLEnum.ArrayBuffer, vbo);
        opengl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
        opengl.EnableVertexAttribArray((uint) alocPos);
        opengl.EnableVertexAttribArray((uint) alocUv);
        opengl.EnableVertexAttribArray((uint) alocColor);
        opengl.VertexAttribPointer((uint) alocPos, 2, GLEnum.Float, false, (uint) sizeof(ImDrawVert), (void*) 0);
        opengl.VertexAttribPointer((uint) alocUv, 2, GLEnum.Float, false, (uint) sizeof(ImDrawVert), (void*) 8);
        opengl.VertexAttribPointer((uint) alocColor, 4, GLEnum.UnsignedByte, true, (uint) sizeof(ImDrawVert), (void*) 16);
    }

    void UpdateTexture(ImTextureDataPtr tex)
    {
        if (tex.Status == ImTextureStatus.WantCreate)
        {
            // remember state
            opengl.GetInteger(GLEnum.TextureBinding2D, out int last_texture);
            
            // create texture based on the ImTextureData
            uint gl_texture_id = opengl.GenTexture();
            opengl.BindTexture(GLEnum.Texture2D, gl_texture_id);
            opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
            opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
            opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
            opengl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
            opengl.PixelStore(GLEnum.UnpackRowLength, 0);
            opengl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba, (uint)tex.Width, (uint)tex.Height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, tex.GetPixels());

            // store identifiers
            tex.SetTexID(new ImTextureID(gl_texture_id));
            tex.SetStatus(ImTextureStatus.Ok);

            // restore state
            opengl.BindTexture(GLEnum.Texture2D, (uint)last_texture);
        }
        else if (tex.Status == ImTextureStatus.WantUpdates)
        {
            // remember state
            opengl.GetInteger(GLEnum.TextureBinding2D, out int last_texture);

            // upload a rectangle of pixels to the existing texture
            opengl.PixelStore(GLEnum.UnpackRowLength, tex.Width);
            opengl.BindTexture(GLEnum.Texture2D, (uint)tex.GetTexID());
            var rect = tex.UpdateRect;
            opengl.TexSubImage2D(GLEnum.Texture2D, 0, rect.X, rect.Y, rect.W, rect.H, GLEnum.Rgba, GLEnum.UnsignedByte, tex.GetPixelsAt(rect.X, rect.Y));
            opengl.PixelStore(GLEnum.UnpackRowLength, 0);

            // set status
            tex.SetStatus(ImTextureStatus.Ok);

            // restore state
            opengl.BindTexture(GLEnum.Texture2D, (uint)last_texture);
        }
        else if (tex.Status == ImTextureStatus.WantDestroy)
        {
            // destroy the texture
            opengl.DeleteTexture((uint)tex.TexID.Handle);
            tex.SetTexID(ImTextureID.Null);
            tex.SetStatus(ImTextureStatus.Destroyed);
        }
    }

    private unsafe void RenderImDrawData(ImDrawData* drawDataPtr)
    {
        int framebufferWidth = (int) (drawDataPtr->DisplaySize.X * drawDataPtr->FramebufferScale.X);
        int framebufferHeight = (int) (drawDataPtr->DisplaySize.Y * drawDataPtr->FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0) return;

        // update the imgui textures
        if (drawDataPtr->Textures != null)
        {
            for (int i = 0; i < drawDataPtr->Textures->Size; i++)
            {
                var textures_vector = *drawDataPtr->Textures;
                var texture = textures_vector[i];
                if (texture.Status != ImTextureStatus.Ok) UpdateTexture(texture);
            }
        }

        opengl.GetInteger(GLEnum.ActiveTexture, out int lastActiveTexture);
        opengl.ActiveTexture(GLEnum.Texture0);

        opengl.GetInteger(GLEnum.CurrentProgram, out int lastProgram);
        opengl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
        opengl.GetInteger(GLEnum.SamplerBinding, out int lastSampler);
        opengl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
        opengl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArrayObject);

        Span<int> lastPolygonMode = stackalloc int[2];
        opengl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
        Span<int> lastScissorBox = stackalloc int[4];
        opengl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

        opengl.GetInteger(GLEnum.BlendSrcRgb, out int lastBlendSrcRgb);
        opengl.GetInteger(GLEnum.BlendDstRgb, out int lastBlendDstRgb);
        opengl.GetInteger(GLEnum.BlendSrcAlpha, out int lastBlendSrcAlpha);
        opengl.GetInteger(GLEnum.BlendDstAlpha, out int lastBlendDstAlpha);
        opengl.GetInteger(GLEnum.BlendEquationRgb, out int lastBlendEquationRgb);
        opengl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEquationAlpha);

        bool lastEnableBlend = opengl.IsEnabled(GLEnum.Blend);
        bool lastEnableCullFace = opengl.IsEnabled(GLEnum.CullFace);
        bool lastEnableDepthTest = opengl.IsEnabled(GLEnum.DepthTest);
        bool lastEnableStencilTest = opengl.IsEnabled(GLEnum.StencilTest);
        bool lastEnableScissorTest = opengl.IsEnabled(GLEnum.ScissorTest);

        SetupRenderState(drawDataPtr);

        Vector2 clipOff = drawDataPtr->DisplayPos;
        Vector2 clipScale = drawDataPtr->FramebufferScale;

        for (int n = 0; n < drawDataPtr->CmdListsCount; n++)
        {
            ImDrawListPtr cmdListPtr = drawDataPtr->CmdLists.Data[n];

            opengl.BufferData(GLEnum.ArrayBuffer, (nuint) (cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), (void*) cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
            opengl.BufferData(GLEnum.ElementArrayBuffer, (nuint) (cmdListPtr.IdxBuffer.Size * sizeof(ushort)), (void*) cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);

            for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr cmdPtr = &cmdListPtr.CmdBuffer.Data[cmd_i];

                Vector4 clipRect;
                clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
                clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
                clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
                clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

                if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
                {
                    opengl.Scissor((int) clipRect.X, (int) (framebufferHeight - clipRect.W), (uint) (clipRect.Z - clipRect.X), (uint) (clipRect.W - clipRect.Y));
                    opengl.BindTexture(GLEnum.Texture2D, (uint)cmdPtr.GetTexID());
                    opengl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*) (cmdPtr.IdxOffset * sizeof(ushort)), (int) cmdPtr.VtxOffset);
                }
            }
        }

        opengl.DeleteVertexArray(vao);
        vao = 0;

        opengl.UseProgram((uint) lastProgram);
        opengl.BindTexture(GLEnum.Texture2D, (uint) lastTexture);
        opengl.BindSampler(0, (uint) lastSampler);
        opengl.ActiveTexture((GLEnum) lastActiveTexture);
        opengl.BindVertexArray((uint) lastVertexArrayObject);
        opengl.BindBuffer(GLEnum.ArrayBuffer, (uint) lastArrayBuffer);
        opengl.BlendEquationSeparate((GLEnum) lastBlendEquationRgb, (GLEnum) lastBlendEquationAlpha);
        opengl.BlendFuncSeparate((GLEnum) lastBlendSrcRgb, (GLEnum) lastBlendDstRgb, (GLEnum) lastBlendSrcAlpha, (GLEnum) lastBlendDstAlpha);

        if (lastEnableBlend) opengl.Enable(GLEnum.Blend);
        else opengl.Disable(GLEnum.Blend);

        if (lastEnableCullFace) opengl.Enable(GLEnum.CullFace);
        else opengl.Disable(GLEnum.CullFace);

        if (lastEnableDepthTest) opengl.Enable(GLEnum.DepthTest);
        else opengl.Disable(GLEnum.DepthTest);

        if (lastEnableStencilTest) opengl.Enable(GLEnum.StencilTest);
        else opengl.Disable(GLEnum.StencilTest);

        if (lastEnableScissorTest) opengl.Enable(GLEnum.ScissorTest);
        else opengl.Disable(GLEnum.ScissorTest);

        opengl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint) lastScissorBox[2], (uint) lastScissorBox[3]);
    }

    private void SetupShaderAndBuffers()
    {
        string vertexSource =
        @"#version 330
        layout (location = 0) in vec2 Position;
        layout (location = 1) in vec2 UV;
        layout (location = 2) in vec4 Color;
        uniform mat4 ProjMtx;
        out vec2 Frag_UV;
        out vec4 Frag_Color;
        void main()
        {
            Frag_UV = UV;
            Frag_Color = Color;
            gl_Position = ProjMtx * vec4(Position.xy,0,1);
        }";

        string fragmentSource =
        @"#version 330
        in vec2 Frag_UV;
        in vec4 Frag_Color;
        uniform sampler2D Texture;
        layout (location = 0) out vec4 Out_Color;
        void main()
        {
            Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
        }";

        shader = new ImGuiShader(opengl, vertexSource, fragmentSource);
        alocTex = shader.GetUniformLocation("Texture");
        alocProj = shader.GetUniformLocation("ProjMtx");
        alocPos = shader.GetAttribLocation("Position");
        alocUv = shader.GetAttribLocation("UV");
        alocColor = shader.GetAttribLocation("Color");

        vbo = opengl.GenBuffer();
        ebo = opengl.GenBuffer();
    }

    private ImGuiKey ToImGuiKey(Key key)
    {
        return key switch
        {
            Key.Tab => ImGuiKey.Tab,
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.PageUp => ImGuiKey.PageUp,
            Key.PageDown => ImGuiKey.PageDown,
            Key.Home => ImGuiKey.Home,
            Key.End => ImGuiKey.End,
            Key.Insert => ImGuiKey.Insert,
            Key.Delete => ImGuiKey.Delete,
            Key.Backspace => ImGuiKey.Backspace,
            Key.Space => ImGuiKey.Space,
            Key.Enter => ImGuiKey.Enter,
            Key.Escape => ImGuiKey.Escape,
            Key.Apostrophe => ImGuiKey.Apostrophe,
            Key.Comma => ImGuiKey.Comma,
            Key.Minus => ImGuiKey.Minus,
            Key.Period => ImGuiKey.Period,
            Key.Slash => ImGuiKey.Slash,
            Key.Semicolon => ImGuiKey.Semicolon,
            Key.Equal => ImGuiKey.Equal,
            Key.LeftBracket => ImGuiKey.LeftBracket,
            Key.BackSlash => ImGuiKey.Backslash,
            Key.RightBracket => ImGuiKey.RightBracket,
            Key.GraveAccent => ImGuiKey.GraveAccent,
            Key.CapsLock => ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiKey.ScrollLock,
            Key.NumLock => ImGuiKey.NumLock,
            Key.PrintScreen => ImGuiKey.PrintScreen,
            Key.Pause => ImGuiKey.Pause,
            Key.Keypad0 => ImGuiKey.Keypad0,
            Key.Keypad1 => ImGuiKey.Keypad1,
            Key.Keypad2 => ImGuiKey.Keypad2,
            Key.Keypad3 => ImGuiKey.Keypad3,
            Key.Keypad4 => ImGuiKey.Keypad4,
            Key.Keypad5 => ImGuiKey.Keypad5,
            Key.Keypad6 => ImGuiKey.Keypad6,
            Key.Keypad7 => ImGuiKey.Keypad7,
            Key.Keypad8 => ImGuiKey.Keypad8,
            Key.Keypad9 => ImGuiKey.Keypad9,
            Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
            Key.KeypadDivide => ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiKey.KeypadAdd,
            Key.KeypadEnter => ImGuiKey.KeypadEnter,
            Key.KeypadEqual => ImGuiKey.KeypadEqual,
            Key.ControlLeft => ImGuiKey.LeftCtrl,
            Key.ShiftLeft => ImGuiKey.LeftShift,
            Key.AltLeft => ImGuiKey.LeftAlt,
            Key.SuperLeft => ImGuiKey.LeftSuper,
            Key.ControlRight => ImGuiKey.RightCtrl,
            Key.ShiftRight => ImGuiKey.RightShift,
            Key.AltRight => ImGuiKey.RightAlt,
            Key.SuperRight => ImGuiKey.RightSuper,
            Key.Menu => ImGuiKey.Menu,
            Key.Number0 => ImGuiKey.Key0,
            Key.Number1 => ImGuiKey.Key1,
            Key.Number2 => ImGuiKey.Key2,
            Key.Number3 => ImGuiKey.Key3,
            Key.Number4 => ImGuiKey.Key4,
            Key.Number5 => ImGuiKey.Key5,
            Key.Number6 => ImGuiKey.Key6,
            Key.Number7 => ImGuiKey.Key7,
            Key.Number8 => ImGuiKey.Key8,
            Key.Number9 => ImGuiKey.Key9,
            Key.A => ImGuiKey.A,
            Key.B => ImGuiKey.B,
            Key.C => ImGuiKey.C,
            Key.D => ImGuiKey.D,
            Key.E => ImGuiKey.E,
            Key.F => ImGuiKey.F,
            Key.G => ImGuiKey.G,
            Key.H => ImGuiKey.H,
            Key.I => ImGuiKey.I,
            Key.J => ImGuiKey.J,
            Key.K => ImGuiKey.K,
            Key.L => ImGuiKey.L,
            Key.M => ImGuiKey.M,
            Key.N => ImGuiKey.N,
            Key.O => ImGuiKey.O,
            Key.P => ImGuiKey.P,
            Key.Q => ImGuiKey.Q,
            Key.R => ImGuiKey.R,
            Key.S => ImGuiKey.S,
            Key.T => ImGuiKey.T,
            Key.U => ImGuiKey.U,
            Key.V => ImGuiKey.V,
            Key.W => ImGuiKey.W,
            Key.X => ImGuiKey.X,
            Key.Y => ImGuiKey.Y,
            Key.Z => ImGuiKey.Z,
            Key.F1 => ImGuiKey.F1,
            Key.F2 => ImGuiKey.F2,
            Key.F3 => ImGuiKey.F3,
            Key.F4 => ImGuiKey.F4,
            Key.F5 => ImGuiKey.F5,
            Key.F6 => ImGuiKey.F6,
            Key.F7 => ImGuiKey.F7,
            Key.F8 => ImGuiKey.F8,
            Key.F9 => ImGuiKey.F9,
            Key.F10 => ImGuiKey.F10,
            Key.F11 => ImGuiKey.F11,
            Key.F12 => ImGuiKey.F12,
            _ => ImGuiKey.None,
        };
    }

    public static void SetupCustomTheme()
    {
        var style = ImGui.GetStyle();
        
        style.Alpha = 1.0f;
        style.DisabledAlpha = 0.6f;
        style.WindowPadding = new Vector2(8.0f, 8.0f);
        style.WindowRounding = 4.0f;
        style.WindowBorderSize = 1.0f;
        style.ChildRounding = 4.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupRounding = 4.0f;
        style.PopupBorderSize = 1.0f;
        style.FrameRounding = 4.0f;
        style.FrameBorderSize = 1.0f;
        style.ItemSpacing = new Vector2(8.0f, 4.0f);
        style.ItemInnerSpacing = new Vector2(4.0f, 4.0f);
        style.CellPadding = new Vector2(4.0f, 2.0f);
        style.ScrollbarSize = 14.0f;
        style.ScrollbarRounding = 4.0f;
        style.GrabMinSize = 10.0f;
        style.GrabRounding = 20.0f;
        style.TabRounding = 4.0f;
        style.TabBorderSize = 1.0f;
        
        style.Colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.4980392158031464f, 0.4980392158031464f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1137254908680916f, 0.1137254908680916f, 0.1137254908680916f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.0784313753247261f, 0.0784313753247261f, 0.0784313753247261f, 0.9399999976158142f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(1.0f, 1.0f, 1.0f, 0.1630901098251343f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.08627451211214066f, 0.08627451211214066f, 0.08627451211214066f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.1529411822557449f, 0.1529411822557449f, 0.1529411822557449f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.1882352977991104f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.1137254908680916f, 0.1137254908680916f, 0.1137254908680916f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.105882354080677f, 0.105882354080677f, 0.105882354080677f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.0f, 0.0f, 0.0f, 0.5099999904632568f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.1137254908680916f, 0.1137254908680916f, 0.1137254908680916f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.01960784383118153f, 0.01960784383118153f, 0.01960784383118153f, 0.5299999713897705f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.3098039329051971f, 0.3098039329051971f, 0.3098039329051971f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.407843142747879f, 0.407843142747879f, 0.407843142747879f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.5098039507865906f, 0.5098039507865906f, 0.5098039507865906f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.8784313797950745f, 0.8784313797950745f, 0.8784313797950745f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.9803921580314636f, 0.9803921580314636f, 0.9803921580314636f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.1490196138620377f, 0.1490196138620377f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.2470588237047195f, 0.2470588237047195f, 0.2470588237047195f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.3294117748737335f, 0.3294117748737335f, 0.3294117748737335f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 0.3098039329051971f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 0.800000011920929f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.4274509847164154f, 0.4274509847164154f, 0.4980392158031464f, 0.5f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.7490196228027344f, 0.7490196228027344f, 0.7490196228027344f, 0.7803921699523926f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.7490196228027344f, 0.7490196228027344f, 0.7490196228027344f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 0.2000000029802322f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.9372549057006836f, 0.9372549057006836f, 0.9372549057006836f, 0.6705882549285889f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.9764705896377563f, 0.9764705896377563f, 0.9764705896377563f, 0.9490196108818054f);
        
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.2235294133424759f, 0.2235294133424759f, 0.2235294133424759f, 0.8627451062202454f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.321568638086319f, 0.321568638086319f, 0.321568638086319f, 0.800000011920929f);
        style.Colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.1450980454683304f, 0.1450980454683304f, 0.1450980454683304f, 0.9725490212440491f);
        style.Colors[(int)ImGuiCol.TabSelected] = new Vector4(0.4235294163227081f, 0.4235294163227081f, 0.4235294163227081f, 1.0f);
        style.Colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(0.2745098173618317f, 0.2745098173618317f, 0.2745098173618317f, 1.0f);
        
        style.Colors[(int)ImGuiCol.TabSelectedOverline] = new Vector4(0.4235294163227081f, 0.4235294163227081f, 0.4235294163227081f, 1.0f);
        style.Colors[(int)ImGuiCol.TabDimmedSelectedOverline] = new Vector4(0.4235294163227081f, 0.4235294163227081f, 0.4235294163227081f, 1.0f);

        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.6078431606292725f, 0.6078431606292725f, 0.6078431606292725f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.0f, 0.4274509847164154f, 0.3490196168422699f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.8980392217636108f, 0.6980392336845398f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.0f, 0.6000000238418579f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.3098039329051971f, 0.3098039329051971f, 0.3490196168422699f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.2274509817361832f, 0.2274509817361832f, 0.2470588237047195f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 0.05999999865889549f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.3499999940395355f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 0.8999999761581421f);
        style.Colors[(int)ImGuiCol.NavCursor] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 0.699999988079071f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.2000000029802322f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.3499999940395355f);
    }
}

struct UniformFieldInfo
{
    public int Location;
    public string Name;
    public int Size;
    public UniformType Type;
}

class ImGuiShader
{
    public uint Program { get; private set; }
    private readonly Dictionary<string, int> _uniformToLocation = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _attribLocation = new Dictionary<string, int>();
    private bool _initialized = false;
    private GL _gl;
    private (ShaderType Type, string Path)[] _files;

    public ImGuiShader(GL gl, string vertexShader, string fragmentShader)
    {
        _gl = gl;
        _files = new[]{
            (ShaderType.VertexShader, vertexShader),
            (ShaderType.FragmentShader, fragmentShader),
        };
        Program = CreateProgram(_files);
    }
    public void UseShader()
    {
        _gl.UseProgram(Program);
    }

    public void Dispose()
    {
        if (_initialized)
        {
            _gl.DeleteProgram(Program);
            _initialized = false;
        }
    }

    public UniformFieldInfo[] GetUniforms()
    {
        _gl.GetProgram(Program, GLEnum.ActiveUniforms, out var uniformCount);

        UniformFieldInfo[] uniforms = new UniformFieldInfo[uniformCount];

        for (int i = 0; i < uniformCount; i++)
        {
            string name = _gl.GetActiveUniform(Program, (uint) i, out int size, out UniformType type);

            UniformFieldInfo fieldInfo;
            fieldInfo.Location = GetUniformLocation(name);
            fieldInfo.Name = name;
            fieldInfo.Size = size;
            fieldInfo.Type = type;

            uniforms[i] = fieldInfo;
        }

        return uniforms;
    }

    public int GetUniformLocation(string uniform)
    {
        if (_uniformToLocation.TryGetValue(uniform, out int location) == false)
        {
            location = _gl.GetUniformLocation(Program, uniform);
            _uniformToLocation.Add(uniform, location);
        }

        return location;
    }

    public int GetAttribLocation(string attrib)
    {
        if (_attribLocation.TryGetValue(attrib, out int location) == false)
        {
            location = _gl.GetAttribLocation(Program, attrib);
            _attribLocation.Add(attrib, location);
        }

        return location;
    }

    private uint CreateProgram(params (ShaderType Type, string source)[] shaderPaths)
    {
        var program = _gl.CreateProgram();

        Span<uint> shaders = stackalloc uint[shaderPaths.Length];
        for (int i = 0; i < shaderPaths.Length; i++)
        {
            shaders[i] = CompileShader(shaderPaths[i].Type, shaderPaths[i].source);
        }

        foreach (var shader in shaders)
            _gl.AttachShader(program, shader);

        _gl.LinkProgram(program);

        _gl.GetProgram(program, GLEnum.LinkStatus, out var success);

        foreach (var shader in shaders)
        {
            _gl.DetachShader(program, shader);
            _gl.DeleteShader(shader);
        }

        _initialized = true;

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var success);
        return shader;
    }
}