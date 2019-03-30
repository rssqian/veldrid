﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreAnimation;
using Foundation;
using UIKit;
using Veldrid;

namespace Veldrid.SampleGallery
{
    public class VeldridUIViewController : UIViewController
    {
        private readonly GraphicsDeviceOptions _options;
        private readonly GraphicsBackend _backend;
        private GraphicsDevice _gd;
        private CADisplayLink _timer;
        private Swapchain _sc;
        private bool _viewLoaded;

        public event Action DeviceCreated;
        public event Action Resized;
        public event Action<double> Rendering;

        public GraphicsDevice Device => _gd;
        public Swapchain MainSwapchain => _sc;

        public VeldridUIViewController()
        {
            bool isDebugBuild = false;
#if DEBUG
            isDebugBuild = true;
#endif

            _options = new GraphicsDeviceOptions(
                debug: isDebugBuild,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: false,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true,
                swapchainSrgbFormat: true);
            _backend = GraphicsBackend.Metal;
        }

        public void Run()
        {
            _timer = CADisplayLink.Create(Render);
            _timer.FrameInterval = 1;
            _timer.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void Render()
        {
            if (_viewLoaded)
            {
                float elapsed = (float)(_timer.TargetTimestamp - _timer.Timestamp);
                Rendering?.Invoke(elapsed);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SwapchainSource ss = SwapchainSource.CreateUIView(this.View.Handle);
            SwapchainDescription scd = new SwapchainDescription(
                ss,
                (uint)View.Frame.Width,
                (uint)View.Frame.Height,
                PixelFormat.R32_Float,
                false);
            if (_backend == GraphicsBackend.Metal)
            {
                _gd = GraphicsDevice.CreateMetal(_options);
                _sc = _gd.ResourceFactory.CreateSwapchain(ref scd);
            }
            else if (_backend == GraphicsBackend.OpenGLES)
            {
                _gd = GraphicsDevice.CreateOpenGLES(_options, scd);
                _sc = _gd.MainSwapchain;
            }
            else if (_backend == GraphicsBackend.Vulkan)
            {
                throw new NotImplementedException();
            }

            DeviceCreated?.Invoke();
            _viewLoaded = true;
        }

        // Called whenever view changes orientation or layout is changed
        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            _sc.Resize((uint)View.Frame.Width, (uint)View.Frame.Height);
            Resized?.Invoke();
        }
    }
}
