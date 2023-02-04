namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    using ModApi.Design;

    public class ProceduralPipeScript : PartModifierScript<ProceduralPipeData>, IDesignerStart
    {
        private Transform meshRoot;

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();

            meshRoot = transform.GetChild(0);

            if(Data.Curve == null)
                Data.Curve = new Curve(Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 1f), Vector3.right);

            GeneratePipeMesh();
        }

        public void DesignerStart(in DesignerFrameData frame)
        {
            PartScript.ConnectedToPart += OnPipeConnected;

            UpdateAttachPoints();
        }
        
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            base.OnSymmetry(mode, originalPart, created);

            UpdatePipeWeights();
            GeneratePipeMesh();
        }
        
        public void UpdatePipeWeights()
        {
            Data.Curve.points[1] = Data.Curve.points[0] + (Data.Curve.points[1] - Data.Curve.points[0]).normalized * Data.FrontWeight;
            Data.Curve.points[2] = Data.Curve.points[3] + (Data.Curve.points[2] - Data.Curve.points[3]).normalized * Data.BackWeight;
        }

        public void UpdateAttachPoints()
        {
            Transform front = Data.Part.AttachPoints[0].AttachPointScript.transform;
            front.position = transform.TransformPoint(Data.Curve.GetPoint(0.0f));
            if (Data.FrontRotation != null)
                front.rotation = Data.FrontRotation;

            Transform back = Data.Part.AttachPoints[1].AttachPointScript.transform;
            back.position = transform.TransformPoint(Data.Curve.GetPoint(1.0f));
            if (Data.BackRotation != null)
                back.rotation = Data.BackRotation;
        }

        private void OnPipeConnected(PartConnectedEventData e)
        {
            if (e.TargetAttachPoint.IsSurfaceAttachPoint) return;
            
            AttachPointScript ps = e.ThisAttachPoint.AttachPointScript;
            AttachPointScript tps = e.TargetAttachPoint.AttachPointScript;
            
            bool isFront = ps.AttachPoint.DisplayName == "Front";
            int first = isFront ? 0 : 3;
            int second = isFront ? 1 : 2;

            Data.Curve.points[first] = meshRoot.InverseTransformPoint(tps.transform.position);
            Data.Curve.points[second] = meshRoot.InverseTransformPoint(tps.transform.position + Data.FrontWeight * tps.transform.forward);

            Vector3 partPos = transform.position;
            transform.position += (Vector3)(transform.localToWorldMatrix * Data.Curve.GetPoint(0.5f));

            for (int i = 0; i < 4; i++)
                Data.Curve.points[i] = Data.Curve.points[i] - (Vector3)(transform.worldToLocalMatrix * (transform.position - partPos));

            ps.transform.rotation = tps.transform.rotation;
            ps.transform.Rotate(new Vector3(0f, 180f, 0f), Space.Self);

            if (isFront)
                Data.FrontRotation = ps.transform.rotation;
            else
                Data.BackRotation = ps.transform.rotation;

            Data.Curve.referenceVector = Vector3.ProjectOnPlane(Random.onUnitSphere, Data.Curve.points[0] - Data.Curve.points[3]).normalized;

            GeneratePipeMesh();
            UpdateAttachPoints();
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
                Data.Curve.DataAtPoint(t, out center, out curveTangent, out curveNormal);
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
                Data.Curve.DataAtPoint(i, out center, out curveTangent, out curveNormal);
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

            Data.Curve.UpdateLength(Data.SegmentCount);
        }
    }
}