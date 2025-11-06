using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Fog;
using SPICA.Formats.CtrH3D.Scene;
using SPICA.Serialization.Attributes;
using System;

namespace SPICA.Formats.CtrGfx.Scene
{
    [TypeChoice(0x00800000, typeof(GfxScene))]
    public class GfxScene : GfxObject
    {
        public override GfxObjRevisionsV5 Revision => GfxObjRevisionsV5.Scene;

        public List<GfxSceneContent> Cameras;
        public List<LightSet> LightSets;
        public List<GfxSceneContent> Fogs;

        public GfxScene()
        {
            Cameras = new List<GfxSceneContent>();
            LightSets = new List<LightSet>();
            Fogs = new List<GfxSceneContent>();
        }

        public class LightSet
        {
            public int ID;

            public List<GfxSceneContent> Lights;

            public LightSet()
            {
                Lights = new List<GfxSceneContent>(); 
            }
        }

        public class GfxSceneContent
        {
            public int ID;
            public string Name;
            public uint Padding; //always 0?
        }

        public H3DScene ToH3D()
        {
            var scene = new H3DScene();

            foreach (var cam in Cameras)
                scene.Cameras.Add(new IndexedName() { Index = cam.ID, Name = cam.Name, });
            foreach (var fog in Fogs)
                scene.Fogs.Add(new IndexedName() { Index = fog.ID, Name = fog.Name, });
            foreach (var set in LightSets)
            {
                IndexedNameArray array = new IndexedNameArray();
                array.Index = set.ID;
                scene.LightSets.Add(array);

                foreach (var l in set.Lights)
                    array.Names.Add(l.Name);
            }
            return scene;
        }

        public void FromH3D(H3DScene scene)
        {
            Cameras.Clear();
            Fogs.Clear();
            LightSets.Clear();

            foreach (var cam in scene.Cameras)
                Cameras.Add(new GfxSceneContent() { Name = cam.Name, ID = cam.Index, });
            foreach (var fog in scene.Fogs)
                Fogs.Add(new GfxSceneContent() { Name = fog.Name, ID = fog.Index, });
            foreach (var set in scene.LightSets)
            {
                LightSet lightSet = new LightSet();
                LightSets.Add(lightSet);
                lightSet.ID = set.Index;
                lightSet.Lights = new List<GfxSceneContent>();

                foreach (var l in set.Names)
                    lightSet.Lights.Add(new GfxSceneContent() { Name = l, });
            }
        }
    }
}
