using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoronoiSplitScreen
{
    public class SplitScreenCamera : MonoBehaviour
    {
        GameObject target;
        RenderTexture stencilBufferRt;
        [SerializeField] int id;
        [SerializeField] Material checkStencil;
        [SerializeField] Material classicMat;
        #region getterSetters
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        #endregion
        private void Awake()
        {
            //stencilBufferRt = new RenderTexture(Screen.width, Screen.height, 24);
        }
        //private void OnRenderImage(RenderTexture source, RenderTexture destination)
        //{
        //    Graphics.Blit(source, destination, checkStencil);
        //}
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            stencilBufferRt = RenderTexture.GetTemporary(Screen.width, Screen.height, 32);
            Graphics.SetRenderTarget(stencilBufferRt.colorBuffer, source.depthBuffer);
            //    // base : 
            //Graphics.Blit(source, destination, checkStencil);

            //// exemple 
            //Graphics.Blit(source, stencilBufferRt, checkStencil);
            //Graphics.Blit(stencilBufferRt, destination);
            Graphics.Blit(source, destination);

            //// allOfIt
            //Graphics.Blit(source, stencilBufferRt, checkStencil);
            //Graphics.Blit(stencilBufferRt, destination, checkStencil);

            //    // test etrange : 
            //Graphics.Blit(stencilBufferRt, destination, checkStencil);
        }
        private void OnPostRender()
        {
            RenderTexture.ReleaseTemporary(stencilBufferRt);
        }


        //public void OnPostRender()
        //{
        //    GL.PushMatrix();
        //    checkStencil.SetPass(0);
        //    GL.LoadOrtho();

        //    GL.Begin(GL.QUADS);
        //    GL.Color(new Color(1, 0, 0, 1));
        //    GL.Vertex3(0, 0, 0);
        //    GL.Vertex3(0, 1, 0);
        //    GL.Vertex3(1, 1, 0);
        //    GL.Vertex3(1, 0, 0);
        //    GL.End();

        //    GL.PopMatrix();
        //}

        //public void OnRenderImage(RenderTexture source, RenderTexture destination)
        //{
        //    stencilBufferRt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        //    Graphics.SetRenderTarget(stencilBufferRt.colorBuffer, source.depthBuffer);
        //    GL.PushMatrix();
        //    //checkStencil.SetTexture("_MainTex", source);
        //    checkStencil.SetPass(0);
        //    GL.LoadOrtho();

        //    GL.Begin(GL.QUADS);
        //    GL.Color(new Color(1, 0, 0, 1));

        //    GL.TexCoord2(0, 0);
        //    GL.Vertex3(0, 0, 0);

        //    GL.TexCoord2(0, 1);
        //    GL.Vertex3(0, 1, 0);

        //    GL.TexCoord2(1, 1);
        //    GL.Vertex3(1, 1, 0);

        //    GL.TexCoord2(1, 0);
        //    GL.Vertex3(1, 0, 0);
        //    GL.End();

        //    GL.PopMatrix();

        //    Graphics.Blit(stencilBufferRt, destination);
        //}



    }

}

