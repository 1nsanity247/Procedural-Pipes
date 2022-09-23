namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Scenes.Events;
    using Assets.Scripts.Flight.GameView.Planet;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    using Craft.Parts.Modifiers.Fuselage;
    using ModApi.Design;

    public class ProceduralPipeScript : PartModifierScript<ProceduralPipeData>, IDesignerStart
    {
        private Transform meshRoot;

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();

            meshRoot = transform.GetChild(0);

            if (Data.points.Length == 5)
            {
                Data.curve.points = Data.points;
                Data.curve.referenceVector = Data.points[4];
            }

            GeneratePipeMesh();
        }

        public void DesignerStart(in DesignerFrameData frame)
        {
            PartScript.ConnectedToPart += ConnectPipe;

            UpdateAttachmentNodes();
        }
        
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            base.OnSymmetry(mode, originalPart, created);

            UpdatePipeWeights();
            GeneratePipeMesh();
        }
        
        public void UpdatePipeWeights()
        {
            Data.curve.points[1] = Data.curve.points[0] + (Data.curve.points[1] - Data.curve.points[0]).normalized * Data.FrontWeight;
            Data.curve.points[2] = Data.curve.points[3] + (Data.curve.points[2] - Data.curve.points[3]).normalized * Data.BackWeight;

            if (Data.points.Length != 5)
                Data.points = new Vector3[5];

            Data.points[1] = Data.curve.points[1];
            Data.points[2] = Data.curve.points[2];
        }

        public void UpdateAttachmentNodes()
        {
            foreach (var point in Data.Part.AttachPoints)
                point.AttachPointScript.transform.position = transform.TransformPoint(Data.curve.GetPoint((point.DisplayName == "Front")? 0:1));
        }

        private void ConnectPipe(PartConnectedEventData e)
        {
            AttachPointScript ps = e.ThisAttachPoint.AttachPointScript;
            AttachPointScript tps = e.TargetAttachPoint.AttachPointScript;

            int first = ps.AttachPoint.DisplayName == "Front" ? 0 : 3;
            int second = ps.AttachPoint.DisplayName == "Front" ? 1 : 2;

            Data.curve.points[first] = meshRoot.InverseTransformPoint(tps.transform.position);
            Data.curve.points[second] = meshRoot.InverseTransformPoint(tps.transform.position + Data.FrontWeight * tps.transform.forward);

            Vector3 partPos = transform.position;
            transform.position += (Vector3)(transform.localToWorldMatrix * (0.5f * (Data.curve.points[0] + Data.curve.points[3])));

            for (int i = 0; i < 4; i++)
                Data.curve.points[i] = Data.curve.points[i] - (Vector3)(transform.worldToLocalMatrix * (transform.position - partPos));

            ps.transform.rotation = tps.transform.rotation;
            ps.transform.Rotate(new Vector3(0f, 180f, 0f), Space.Self);

            Vector3 refVec = Vector3.zero;
            Vector3 dir = (Data.curve.points[0] - Data.curve.points[3]).normalized;

            while (refVec == Vector3.zero) { 
                refVec = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                refVec -= Vector3.Dot(refVec, dir) * dir;
            }

            Data.curve.referenceVector = refVec;

            if (Data.points.Length != 5)
                Data.points = new Vector3[5];

            for (int i = 0; i < 4; i++)
                Data.points[i] = Data.curve.points[i];
            Data.points[4] = Data.curve.referenceVector;

            GeneratePipeMesh();

            UpdateAttachmentNodes();
        }

        public void GeneratePipeMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> uvs = new List<Vector4>();
            List<int> triangles = new List<int>();

            int res = Data.Resolution;
            Vector3 center, curveTangent, curveNormal, curveBinormal;
            Vector2 offset = new Vector2();

            float w = Data.Part.MaterialIds[0] * (1.0f/99.0f);

            for (int i = 0; i < Data.SegmentCount + 1; i++)
            {
                float t = (float)i / Data.SegmentCount;
                Data.curve.DataAtPoint(t, out center, out curveTangent, out curveNormal);
                curveBinormal = Vector3.Cross(curveNormal, curveTangent);

                for (int x = 0; x < res; x++)
                {
                    offset.Set(Mathf.Sin((x * 2 * Mathf.PI) / res), Mathf.Cos((x * 2 * Mathf.PI) / res));
                    vertices.Add(center + Mathf.Lerp(Data.FrontRadius, Data.BackRadius, t) * (curveNormal * offset.x + curveBinormal * offset.y));
                    normals.Add(Vector3.Normalize(vertices[i * res + x] - center));
                    uvs.Add(new Vector4(0.0f, 0.0f, 0.0f, w));

                    if (i != 0 && x > 0)
                    {
                        triangles.Add(i * res + x - 1); triangles.Add((i - 1) * res + x - 1); triangles.Add(i * res + x);
                        triangles.Add(i * res + x); triangles.Add((i - 1) * res + x - 1); triangles.Add((i - 1) * res + x);

                        if (x == res - 1)
                        {
                            triangles.Add(i * res + x); triangles.Add((i - 1) * res + x); triangles.Add(i * res);
                            triangles.Add(i * res); triangles.Add((i - 1) * res + x); triangles.Add((i - 1) * res);
                        }
                    }
                }
            }

            for (int i = 0; i < 2; i++)
            {
                int currVert = vertices.Count;
                Data.curve.DataAtPoint(i, out center, out curveTangent, out curveNormal);
                curveBinormal = Vector3.Cross(curveNormal, curveTangent);

                vertices.Add(center);
                normals.Add((i == 0 ? -1 : 1) * curveTangent);
                uvs.Add(new Vector4(0.0f, 0.0f, 0.0f, w));

                for (int x = 0; x < res; x++)
                {
                    offset.Set(Mathf.Sin((x * 2 * Mathf.PI) / res), Mathf.Cos((x * 2 * Mathf.PI) / res));
                    vertices.Add(center + Mathf.Lerp(Data.FrontRadius, Data.BackRadius, i) * (curveNormal * offset.x + curveBinormal * offset.y));
                    normals.Add((i==0?-1:1) * curveTangent);
                    uvs.Add(new Vector4(0.0f, 0.0f, 0.0f, w));

                    if (x != 0)
                    {
                        triangles.Add(currVert); triangles.Add(currVert + x + (i==1?0:1)); triangles.Add(currVert + x + (i==1?1:0));

                        if(x == res - 1)
                        {
                            triangles.Add(currVert); triangles.Add(currVert + (i==1?x+1:1)); triangles.Add(currVert + (i==1?1:x+1));
                        }
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            meshRoot.GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRoot.GetComponent<MeshCollider>().sharedMesh = mesh;

            UpdatePipeLength();
        }

        public void UpdatePipeLength()
        {
            float length = 0;

            for (int i = 0; i < Data.SegmentCount; i++)
                length += (Data.curve.GetPoint((i + 1) / Data.SegmentCount) - Data.curve.GetPoint(i / Data.SegmentCount)).magnitude;

            Data.length = length;
        }
    }
}