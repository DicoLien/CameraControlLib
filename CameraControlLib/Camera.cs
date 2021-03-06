﻿using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace CameraControlLib
{
    /// <summary>
    /// Provides access to the properties of a camera.
    /// </summary>
    /// <remarks>
    /// To create a Camera, use Camera.GetAll() to enumerate the 
    /// available video capture devices. This method returns a list of 
    /// camera descriptors and calling the Create() method on the camera
    /// descriptor will instantiate the Camera device.
    /// 
    /// The descriptors don't store any unmanaged resources and so don't need
    /// resource cleanup, but the Camera object should be disposed when 
    /// finished with it.
    /// </remarks>
    public class Camera : IDisposable
    {
        private DsDevice _device;
        private IBaseFilter _filter;
        private Dictionary<string, CameraProperty> _cameraProperties = new Dictionary<string, CameraProperty>();

        public CameraProperty Focus { get { return Get("Focus"); } }
        public CameraProperty Exposure { get { return Get("Exposure"); } }
        public CameraProperty Zoom { get { return Get("Zoom"); } }
        public CameraProperty Pan { get { return Get("Pan"); } }
        public CameraProperty Tilt { get { return Get("Tilt"); } }
        public CameraProperty Roll { get { return Get("Roll"); } }
        public CameraProperty Iris { get { return Get("Iris"); } }

        public CameraProperty Brightness { get { return Get("Brightness"); } }
        public CameraProperty Contrast { get { return Get("Contrast"); } }
        public CameraProperty Hue { get { return Get("Hue"); } }
        public CameraProperty Saturation { get { return Get("Saturation"); } }
        public CameraProperty Sharpness { get { return Get("Sharpness"); } }
        public CameraProperty Gamma { get { return Get("Gamma"); } }
        public CameraProperty ColorEnable { get { return Get("ColorEnable"); } }
        public CameraProperty WhiteBalance { get { return Get("WhiteBalance"); } }
        public CameraProperty BacklightCompensation { get { return Get("BacklightCompensation"); } }
        public CameraProperty Gain { get { return Get("Gain"); } }

        internal IBaseFilter Filter { get { return _filter; } }

        internal Camera(DsDevice device)
        {
            _device = device;
            IFilterGraph2 graphBuilder = new FilterGraph() as IFilterGraph2;
            IMoniker i = _device.Mon as IMoniker;
            graphBuilder.AddSourceFilterForMoniker(i, null, _device.Name, out _filter);

            RegisterProperties();
        }

        private readonly static List<CameraPropertyDescriptor> s_knownProperties = new List<CameraPropertyDescriptor>()
        {
            CamControlProperty.CreateDescriptor("Focus", "Focus", CameraControlProperty.Focus),
            CamControlProperty.CreateDescriptor("Exposure", "Exposure time", CameraControlProperty.Exposure),
            CamControlProperty.CreateDescriptor("Zoom", "Zoom", CameraControlProperty.Zoom),
            CamControlProperty.CreateDescriptor("Pan", "Pan", CameraControlProperty.Pan),
            CamControlProperty.CreateDescriptor("Tilt", "Tilt", CameraControlProperty.Tilt),
            CamControlProperty.CreateDescriptor("Roll", "Roll", CameraControlProperty.Roll),
            CamControlProperty.CreateDescriptor("Iris", "Iris", CameraControlProperty.Iris),

            VideoProcAmpCameraProperty.CreateDescriptor("Brightness", "Brightness", VideoProcAmpProperty.Brightness),
            VideoProcAmpCameraProperty.CreateDescriptor("Contrast", "Contrast", VideoProcAmpProperty.Contrast),
            VideoProcAmpCameraProperty.CreateDescriptor("Hue", "Hue", VideoProcAmpProperty.Hue),
            VideoProcAmpCameraProperty.CreateDescriptor("Saturation", "Saturation", VideoProcAmpProperty.Saturation),
            VideoProcAmpCameraProperty.CreateDescriptor("Sharpness", "Sharpness", VideoProcAmpProperty.Sharpness),
            VideoProcAmpCameraProperty.CreateDescriptor("Gamma", "Gamma", VideoProcAmpProperty.Gamma),
            VideoProcAmpCameraProperty.CreateDescriptor("ColorEnable", "Color Enable", VideoProcAmpProperty.ColorEnable),
            VideoProcAmpCameraProperty.CreateDescriptor("WhiteBalance", "White Balance", VideoProcAmpProperty.WhiteBalance),
            VideoProcAmpCameraProperty.CreateDescriptor("BacklightCompensation", "Backlight Compensation", VideoProcAmpProperty.BacklightCompensation),
            VideoProcAmpCameraProperty.CreateDescriptor("Gain", "Gain", VideoProcAmpProperty.Gain)
        };

        public CameraProperty Get(string propertyId)
        {
            return _cameraProperties[propertyId];
        }

        public static System.Collections.Generic.IReadOnlyList<CameraPropertyDescriptor> KnownProperties
        {
            get { return s_knownProperties.AsReadOnly(); }
        }

        private void RegisterProperties()
        {
            foreach (var descriptor in Camera.KnownProperties)
            {
                _cameraProperties[descriptor.Id] = descriptor.Create(this);
            }
        }

        /// <summary>
        /// Gets a list of all the available properties, even if the camera doesn't appear to support them.
        /// </summary>
        /// <returns></returns>
        public List<CameraProperty> GetProperties()
        {
            return _cameraProperties.Values.ToList();
        }

        /// <summary>
        /// Gets a list of the properties supported by this camera.
        /// </summary>
        /// <returns></returns>
        public List<CameraProperty> GetSupportedProperties()
        {
            return _cameraProperties.Values.Where(p => p.Supported).ToList();
        }

        /// <summary>
        /// Fetches updated 
        /// </summary>
        public void Refresh()
        {
            foreach (var prop in _cameraProperties.Values)
                prop.Refresh();
        }

        public void Save()
        {
            foreach (var prop in _cameraProperties.Values)
                prop.Save();
        }

        public void ResetToDefault()
        {
            foreach (var prop in _cameraProperties.Values)
                prop.ResetToDefault();
        }

        public static IList<CameraDescriptor> GetAll()
        {
            return CameraDescriptor.GetAll();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                _disposedValue = true;
            }
        }

        ~Camera()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }


    public interface ICameraDescriptor
    {
        string Name { get; }
        string DevicePath { get; }
    }


    /// <summary>
    /// Represents an available Camera device, but has no unmanaged resources associated
    /// </summary>
    public class CameraDescriptor : ICameraDescriptor
    {
        public string Name { get; private set; }
        public string DevicePath { get; private set; }

        private CameraDescriptor() { }

        public Camera Create()
        {
            DsDevice matchingDevice = null;
            DsDevice[] cameraDevices = null;
            try
            {
                cameraDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                var exactMatch = cameraDevices.FirstOrDefault(d => d.Name == Name && d.DevicePath == DevicePath);
                var nameMatch = cameraDevices.FirstOrDefault(d => d.Name == Name);
                matchingDevice = exactMatch ?? nameMatch;
                if (matchingDevice == null)
                    throw new InvalidOperationException("Could not find selected camera device");
                return new Camera(matchingDevice);
            }
            finally
            {
                DisposeDevices(cameraDevices, deviceToKeep: matchingDevice);
            }
        }

        public static List<CameraDescriptor> GetAll()
        {
            DsDevice[] cameraDevices = null;
            try
            {
                return DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice).Select(d => CameraDescriptor.FromDsDevice(d)).ToList();
            }
            finally
            {
                DisposeDevices(cameraDevices);
            }
        }

        /// <summary>
        /// Attempts to find a camera by its name and device path.
        /// 
        /// If both name and devicePath do not match, it will fall back to 
        /// identifying the camera by name alone.
        /// </summary>
        /// <param name="name">The name of the camera</param>
        /// <param name="devicePath">The device path for the camera</param>
        /// <returns>A matching camera descriptor, or null if the camera is not found</returns>
        public static CameraDescriptor Find(string name, string devicePath)
        {
            var cameraDescriptors = CameraDescriptor.GetAll();
            var exactMatch = cameraDescriptors.FirstOrDefault(c => c.Name == name && c.DevicePath == devicePath);
            var nameMatch = cameraDescriptors.FirstOrDefault(c => c.Name == name);
            return exactMatch ?? nameMatch;
        }

        internal static CameraDescriptor FromDsDevice(DsDevice device)
        {
            return new CameraDescriptor() { Name = device.Name, DevicePath = device.DevicePath };
        }

        private static void DisposeDevices(IEnumerable<DsDevice> devices, DsDevice deviceToKeep = null)
        {
            if (devices != null)
            {
                foreach (var device in devices)
                    if (device != null && device != deviceToKeep)
                        device.Dispose();
            }
        }
    }
}
