using System;
using System.Reflection;
using ColossalFramework.UI;
using ICities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SampleMod
{
    public class MyPanel : UIPanel
    {
        public override void Start()
        {
            Mod.Log("MyPanel::Start()");
            base.Start();
        }

        public override void OnDestroy()
        {
            Mod.Log("MyPanel::OnDestroy()");
            base.OnDestroy();
        }
    }

    public class Mod : IUserMod
    {
        public string Name { get { return "SampleMod"; } }
        public string Description { get { return "SampleMod for ProxyClassGenerator"; } }

        public static void Log(string message)
        {
            DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, message);
        }

        public static Type[] ProxyTypes
        {
            get
            {
                return new[] { typeof(MyPanel) };
            }
        }

        private static ProxyLoader _proxyLoader = null;

        public static Type GetProxy<T>()
        {
            _proxyLoader = _proxyLoader ?? new ProxyLoader(@"d:\.projects\SampleMod\SampleMod\bin\Debug\SampleModProxy.dll");
            return _proxyLoader.GetProxy<T>();
        }
    }

    public class ModLoader : LoadingExtensionBase
    {
        private LoadMode _mode;

        private const string PanelGameObjectName = "MyPanelGameObject";

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            Mod.Log("OnCreated, current assembly: " + Assembly.GetExecutingAssembly().FullName);
            DestroyObjects();
            CreateObjects();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            _mode = mode;
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            Mod.Log("OnLevelLoaded");
            CreateObjects();
        }

        public override void OnLevelUnloading()
        {
            if (_mode != LoadMode.LoadGame && _mode != LoadMode.NewGame)
                return;

            Mod.Log("OnLevelUnloading");
            DestroyObjects();
        }

        private void CreateObjects()
        {
            var gameObject = new GameObject(PanelGameObjectName);
            var myPanelObject = gameObject.AddComponent(Mod.GetProxy<MyPanel>());
            Mod.Log("AddComponent returned type: " + myPanelObject.GetType().AssemblyQualifiedName);

            try
            {
                var myPanel = (MyPanel)myPanelObject;
                Mod.Log("Cast successful");
            }
            catch (InvalidCastException e)
            {
                Mod.Log("Invalid cast: " + e.Message);
            }
        }

        private void DestroyObjects()
        {
            var panelGameObject = GameObject.Find(PanelGameObjectName);
            if (panelGameObject != null)
            {
                Object.Destroy(panelGameObject);
            }
        }
    }
}