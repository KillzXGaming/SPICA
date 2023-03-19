using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Math3D;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SPICA.Formats.CtrH3D.Model
{
    public class H3DModel : INamed, ICustomSerialization
    {
        public H3DModelFlags Flags;
        public H3DBoneScaling BoneScaling;

        public ushort SilhouetteMaterialsCount;

        public Matrix3x4 WorldTransform;

        public readonly H3DDict<H3DMaterial> Materials;

        public readonly List<H3DMesh> Meshes;

        [Range] public readonly List<H3DMesh> MeshesLayer0;
        [Range] public readonly List<H3DMesh> MeshesLayer1;
        [Range] public readonly List<H3DMesh> MeshesLayer2;
        [Range] public readonly List<H3DMesh> MeshesLayer3;

        [IfVersion(CmpOp.Gequal, 7)] public readonly List<H3DSubMeshCulling> SubMeshCullings;

        public readonly H3DDict<H3DBone> Skeleton;

        public readonly List<bool> MeshNodesVisibility;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        public int MeshNodesCount;

        public H3DPatriciaTree MeshNodesTree;

#pragma warning disable CS0414 // Le champ 'H3DModel.UserDefinedAddress' est assigné, mais sa valeur n'est jamais utilisée
        private uint UserDefinedAddress;
#pragma warning restore CS0414 // Le champ 'H3DModel.UserDefinedAddress' est assigné, mais sa valeur n'est jamais utilisée

        public H3DMetaData MetaData;

        public H3DModel()
        {
            WorldTransform = Matrix3x4.Identity;

            Materials = new H3DDict<H3DMaterial>();

            Meshes = new List<H3DMesh>();

            MeshesLayer0 = new List<H3DMesh>();
            MeshesLayer1 = new List<H3DMesh>();
            MeshesLayer2 = new List<H3DMesh>();
            MeshesLayer3 = new List<H3DMesh>();

            SubMeshCullings = new List<H3DSubMeshCulling>();

            Skeleton = new H3DDict<H3DBone>();

            MeshNodesVisibility = new List<bool>();
            MeshNodesTree = new H3DPatriciaTree();

            UserDefinedAddress = 0; //SBZ, set by program on 3DS
        }

        public void AddMesh(H3DMesh Mesh)
        {
            Mesh.Parent = this;

            Meshes.Add(Mesh);

            switch (Mesh.Layer)
            {
                case 0: MeshesLayer0.Add(Mesh); break;
                case 1: MeshesLayer1.Add(Mesh); break;
                case 2: MeshesLayer2.Add(Mesh); break;
                case 3: MeshesLayer3.Add(Mesh); break;

                default: throw new ArgumentOutOfRangeException("Invalid Layer! Expected 0, 1, 2 or 3!");
            }
        }

        public void AddMeshes(IEnumerable<H3DMesh> Meshes)
        {
            foreach (H3DMesh Mesh in Meshes) AddMesh(Mesh);
        }

        public void AddMeshes(params H3DMesh[] Meshes)
        {
            foreach (H3DMesh Mesh in Meshes) AddMesh(Mesh);
        }

        public void RemoveMesh(H3DMesh Mesh)
        {
            if (Meshes.Remove(Mesh))
            {
                MeshesLayer0.Remove(Mesh);
                MeshesLayer1.Remove(Mesh);
                MeshesLayer2.Remove(Mesh);
                MeshesLayer3.Remove(Mesh);
            }
        }

        public void ClearMeshes()
        {
            Meshes.Clear();

            MeshesLayer0.Clear();
            MeshesLayer1.Clear();
            MeshesLayer2.Clear();
            MeshesLayer3.Clear();
        }

        void ICustomSerialization.Deserialize(BinaryDeserializer Deserializer)
        {
        }

        bool ICustomSerialization.Serialize(BinarySerializer Serializer)
        {
            int LastRenderLayer = 0;
            foreach (H3DMesh Mesh in Meshes)
            {
                if (Mesh.Layer > LastRenderLayer)
                {
                    LastRenderLayer = Mesh.Layer;
                }
                else if (Mesh.Layer < LastRenderLayer)
                {
                    Console.WriteLine("WARNING: Meshes not sorted. Fixing...");
                    List<H3DMesh> SortedMeshes = Meshes.OrderBy(SortedMesh => SortedMesh.Layer).ToList();
                    ClearMeshes();
                    AddMeshes(SortedMeshes);
                    break;
                }
            }
            MeshNodesCount = MeshNodesTree.Count;
            return false;
        }
    }
}
