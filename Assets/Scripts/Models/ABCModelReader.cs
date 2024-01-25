using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using Utility;
using static ABCModelReader;

public class ABCModelReader : MonoBehaviour
{
    private int nVersion;
    private int nNodeCount;
    private int nLODCount;
    private int nWeightSetCount;

    public bool bAllLODs = false;

    public Model model;

    public Importer importer;

    public void Start()
    {
        //always make sure we have a reference to the Importer, this is basically like a global object that holds important path info.
        importer = GameObject.Find("Level").GetComponent<Importer>();
    }


    private Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }


    private string ReadString(BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        byte[] stringBytes = reader.ReadBytes(length);
        return System.Text.Encoding.ASCII.GetString(stringBytes);
    }

    private Weight ReadWeight(BinaryReader reader)
    {
        Weight weight = new Weight();
        weight.NodeIndex = reader.ReadUInt32();
        weight.Location = ReadVector3(reader);
        weight.Bias = reader.ReadSingle();
        return weight;
    }

    private Vertex ReadVertex(BinaryReader reader)
    {
        Vertex vertex = new Vertex();
        int nWeightCount = reader.ReadUInt16();
        vertex.SublodVertexIndex = reader.ReadUInt16();
        vertex.Weights = new List<Weight>();
        for (int i = 0; i < nWeightCount; i++)
        {
            vertex.Weights.Add(ReadWeight(reader));
        }
        vertex.Location = ReadVector3(reader);
        vertex.Normal = ReadVector3(reader);
        return vertex;
    }

    private FaceVertex ReadFaceVertex(BinaryReader reader)
    {
        FaceVertex faceVertex = new FaceVertex();
        faceVertex.Texcoord.x = reader.ReadSingle();
        faceVertex.Texcoord.y = reader.ReadSingle();
        faceVertex.VertexIndex = reader.ReadUInt16();
        return faceVertex;
    }

    private Face ReadFace(BinaryReader reader)
    {
        Face face = new Face();
        face.Vertices = new List<FaceVertex>();
        for (int i = 0; i < 3; i++)
        {
            face.Vertices.Add(ReadFaceVertex(reader));
        }
        return face;
    }

    private LOD ReadLOD(BinaryReader reader)
    {
        LOD lod = new LOD();
        int nFaceCount = reader.ReadInt32();
        lod.Faces = new List<Face>();
        for (int i = 0; i < nFaceCount; i++)
        {
            lod.Faces.Add(ReadFace(reader));
        }
        int nVertexCount = reader.ReadInt32();
        lod.Vertices = new List<Vertex>();
        for (int i = 0; i < nVertexCount; i++)
        {
            lod.Vertices.Add(ReadVertex(reader));
        }
        return lod;
    }

    private Piece ReadPiece(BinaryReader reader)
    {
        Piece piece = new Piece();
        piece.MaterialIndex = reader.ReadUInt16();
        piece.SpecularPower = reader.ReadSingle();
        piece.SpecularScale = reader.ReadSingle();
        if (nVersion > 9)
        {
            piece.LodWeight = reader.ReadSingle();
        }
        piece.Padding = reader.ReadUInt16();
        piece.Name = ReadString(reader);
        piece.LODs = new List<LOD>();
        for (int i = 0; i < nLODCount; i++)
        {
            piece.LODs.Add(ReadLOD(reader));
        }
        return piece;
    }
   
    public GameObject LoadABC(ModelDefinition mDef)
    {

        if (mDef == null)
        {
            return null;
        }

        Model model = new Model();
        model.Name = Path.GetFileNameWithoutExtension(mDef.szModelFilePath);

        if (mDef.szModelFilePath == null)
        {
            throw new Exception("No file selected");
        }

        if (!File.Exists(mDef.szModelFilePath))
        {
            return null;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(mDef.szModelFilePath)))
        {
            int nextSectionOffset = 0;
            while (nextSectionOffset != -1)
            {
                reader.BaseStream.Seek(nextSectionOffset, SeekOrigin.Begin);
                string sectionName = ReadString(reader);
                nextSectionOffset = reader.ReadInt32();
                if (sectionName == "Header")
                {
                    nVersion = reader.ReadInt32();
                    if (nVersion < 9 || nVersion > 13)
                    {
                        throw new Exception($"Unsupported file version ({nVersion}).");
                    }
                    model.Version = nVersion;
                    reader.BaseStream.Seek(8, SeekOrigin.Current);
                    nNodeCount = reader.ReadInt32();
                    reader.BaseStream.Seek(20, SeekOrigin.Current);
                    nLODCount = reader.ReadInt32();
                    reader.BaseStream.Seek(4, SeekOrigin.Current);
                    nWeightSetCount = reader.ReadInt32();
                    reader.BaseStream.Seek(8, SeekOrigin.Current);

                    // Unknown new value
                    if (nVersion >= 13)
                    {
                        reader.BaseStream.Seek(4, SeekOrigin.Current);
                    }

                    model.CommandString = ReadString(reader);
                    model.InternalRadius = reader.ReadSingle();
                    reader.BaseStream.Seek(64, SeekOrigin.Current);
                    model.LODDistances = new List<float>();
                    for (int i = 0; i < nLODCount; i++)
                    {
                        model.LODDistances.Add(reader.ReadSingle());
                    }
                }
                else if (sectionName == "Pieces")
                {
                    int nWeightCount = reader.ReadInt32();
                    int nPiecesCount = reader.ReadInt32();
                    model.Pieces = new List<Piece>();
                    for (int i = 0; i < nPiecesCount; i++)
                    {
                        model.Pieces.Add(ReadPiece(reader));
                    }
                }
            }
            reader.Close();
        }
        this.model = model;


        if(importer == null)
        {
            importer = GameObject.FindObjectOfType<Importer>();
        }

        //load dtx textures
        foreach (var tex in mDef.szModelTextureName)
        {
            DTX.LoadDTX(importer.projectPath + "\\" + tex, ref importer.dtxMaterialList, importer.projectPath);
        }

        mDef.model = model;

        mDef.rootObject = new GameObject(model.Name);
        mDef.rootObject.transform.position = Vector3.zero;
        mDef.rootObject.transform.rotation = Quaternion.identity;
        mDef.rootObject.transform.localScale = Vector3.one;

        foreach (var m in model.Pieces)
        {
            GameObject modelInGameObject = new GameObject(m.Name);
            modelInGameObject.transform.parent = mDef.rootObject.transform;
            modelInGameObject.AddComponent<MeshFilter>();
            modelInGameObject.AddComponent<MeshRenderer>();
            modelInGameObject.GetComponent<MeshFilter>().mesh = CreateMesh(m, mDef.modelType);

            modelInGameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();

            //Sometimes people don't specify a second, third or fourth texture... so we need to check if the index is out of bounds
            if (m.MaterialIndex > mDef.szModelTextureName.Count - 1)
                m.MaterialIndex = (ushort)(mDef.szModelTextureName.Count - 1);

            modelInGameObject.GetComponent<MeshRenderer>().material = importer.dtxMaterialList.materials[Path.GetFileName(mDef.szModelTextureName[m.MaterialIndex])];
            
            if(mDef.bChromakey)
            {
                var mr = modelInGameObject.GetComponent<MeshRenderer>();
                mr.material.shader = Shader.Find("Shader Graphs/Lithtech Vertex Transparent");
                mr.material.SetInt("_Chromakey", 1);

            }
        }

        //combine
        mDef.rootObject.MeshCombine(true);

        mDef.rootObject.tag = "NoRayCast";

        if (mDef.bMoveToFloor || mDef.modelType == ModelType.Pickup || mDef.modelType == ModelType.Character || mDef.modelType == ModelType.Weapon)
            mDef.rootObject.AddComponent<DebugLines>();

        

        mDef.rootObject.transform.SetParent(GameObject.Find("Models").transform);

        return mDef.rootObject;
    }


    public Mesh CreateMesh(Piece piece, ModelType type)
    {
        List<Mesh> individualMeshes = new List<Mesh>();

        // only use the first LOD, we don't care about the rest.
        //foreach (LOD lod in piece.LODs)
        //{
        foreach (Face face in piece.LODs[0].Faces)
            {
                Mesh faceMesh = new Mesh();
                List<Vector3> faceVertices = new List<Vector3>();
                List<Vector3> faceNormals = new List<Vector3>();
                List<Vector2> faceUV = new List<Vector2>();
                List<int> faceTriangles = new List<int>();

                foreach (FaceVertex faceVertex in face.Vertices)
                {
                    int originalVertexIndex = faceVertex.VertexIndex;

                    // Add vertices, normals, and UVs for the current face
                    faceVertices.Add(piece.LODs[0].Vertices[originalVertexIndex].Location * 0.01f);
                    faceNormals.Add(piece.LODs[0].Vertices[originalVertexIndex].Normal);


                Vector2 flippedUV = new Vector2(faceVertex.Texcoord.x, faceVertex.Texcoord.y);
                if (faceVertex.Texcoord.y < 0.0f)
                {
                    flippedUV.y = 1.0f - flippedUV.y;
                }
                    faceUV.Add(flippedUV);
                    faceTriangles.Add(faceVertices.Count - 1);
                }

                faceMesh.vertices = faceVertices.ToArray();
                faceMesh.normals = faceNormals.ToArray();
                faceMesh.uv = faceUV.ToArray();
                faceMesh.triangles = faceTriangles.ToArray();

                individualMeshes.Add(faceMesh);
            }
        // } // only use the first LOD, we don't care about the rest.

        // Combine all individual meshes into a single mesh
        Mesh combinedMesh = CombineMeshes(individualMeshes.ToArray());

        return combinedMesh;
    }

    private Mesh CombineMeshes(Mesh[] meshes)
    {
        CombineInstance[] combineInstances = new CombineInstance[meshes.Length];

        for (int i = 0; i < meshes.Length; i++)
        {
            combineInstances[i].mesh = meshes[i];
            combineInstances[i].transform = Matrix4x4.identity;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances, true, true);

        return combinedMesh;
    }
}


public class Model
{
    public string Name;
    public int Version;
    public string CommandString;
    public float InternalRadius;
    public List<float> LODDistances;
    public List<Piece> Pieces;
}

public class Weight
{
    public uint NodeIndex;
    public Vector3 Location;
    public float Bias;
}

public class Vertex
{
    public ushort SublodVertexIndex;
    public List<Weight> Weights;
    public Vector3 Location;
    public Vector3 Normal;
}

public class FaceVertex
{
    public Vector2 Texcoord;
    public ushort VertexIndex;
}

public class Face
{
    public List<FaceVertex> Vertices;
}

public class LOD
{
    public List<Face> Faces;
    public List<Vertex> Vertices;
}

public class Piece
{
    public ushort MaterialIndex;
    public float SpecularPower;
    public float SpecularScale;
    public float LodWeight;
    public ushort Padding;
    public string Name;
    public List<LOD> LODs;
}