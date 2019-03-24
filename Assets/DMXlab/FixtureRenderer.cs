using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DMXlab
{
    [RequireComponent(typeof(Camera))]
    public class FixtureRenderer : MonoBehaviour 
    {
        public Fixture fixture;
        public int pixelStart;

        public enum PixelFormat { RGB, RGBW }
        public PixelFormat pixelFormat;
        public Vector2Int resolution;

        RenderTexture _renderTexture;
        Texture2D _texture;
        Camera _camera;

        void Start()
        {
            _camera = GetComponent<Camera>();

            _renderTexture = new RenderTexture(resolution.x, resolution.y, 24);
            _texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);

            _camera.targetTexture = _renderTexture;
        }

        void OnPostRender()
        {
            RenderTexture.active = _renderTexture;
            _texture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            _texture.Apply();

            Color[] pixels = _texture.GetPixels();

            int channel = pixelStart;
            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    Color pixel = Color.white;

                    float u = (float)x / resolution.x;
                    float v = (float)(resolution.y - 1 - y) / resolution.y;

                    int pixelX = (int)(u * _texture.width);
                    int pixelY = (int)(v * _texture.height);

                    pixel = pixels[pixelY * _texture.width + pixelX];

                    fixture.SetChannelValue(channel++, (byte)(pixel.r * 255));
                    fixture.SetChannelValue(channel++, (byte)(pixel.g * 255));
                    fixture.SetChannelValue(channel++, (byte)(pixel.b * 255));
                    if (pixelFormat == PixelFormat.RGBW)
                        fixture.SetChannelValue(channel++, (byte)(pixel.a * 255));
                }
            }
        }
    }
}