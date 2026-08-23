#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class UniGif
{
    public struct DecodedFrame
    {
        public int width;
        public int height;
        public float delaySec;
        public Color32[] pixels;
    }

    /// <summary>
    /// Thread-safe: Parses GIF bytes, LZW decompression, color tables, and delta compositing.
    /// Handles disposal methods 1, 2, and 3 to prevent frame overlapping/ghosting.
    /// </summary>
    public static DecodedFrame[] DecodeThreadSafe(byte[] bytes, bool debugLog = false)
    {
        var gifData = new GifData();
        if (!SetGifData(bytes, ref gifData, debugLog))
        {
            return null;
        }

        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
        {
            return null;
        }

        int canvasWidth = gifData.m_logicalScreenWidth;
        int canvasHeight = gifData.m_logicalScreenHeight;

        var decodedFrames = new List<DecodedFrame>(gifData.m_imageBlockList.Count);

        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32[] currentCanvas = new Color32[canvasWidth * canvasHeight];
        Color32[] savedCanvasForDisposal3 = new Color32[canvasWidth * canvasHeight];

        // Initialize empty canvas as fully transparent
        for (int i = 0; i < currentCanvas.Length; i++)
        {
            currentCanvas[i] = transparent;
        }

        ImageBlock? prevImageBlock = null;
        ushort prevDisposalMethod = 0;

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            ImageBlock imageBlock = gifData.m_imageBlockList[i];
            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, i);
            ushort disposalMethod = GetDisposalMethod(graphicCtrlEx);
            int transparentIndex = GetTransparentIndex(graphicCtrlEx);

            Color32 bgColor;
            List<byte[]> colorTable = GetColorTableAndSetBgColor(gifData, imageBlock, transparentIndex, out bgColor);

            // 1. Process previous frame disposal method before rendering the current frame
            if (i > 0)
            {
                if (prevDisposalMethod == 2 && prevImageBlock.HasValue)
                {
                    // Disposal 2: Clear region occupied by previous frame back to transparent
                    ClearImageBlockRegion(currentCanvas, canvasWidth, canvasHeight, prevImageBlock.Value, transparent);
                }
                else if (prevDisposalMethod == 3)
                {
                    // Disposal 3: Restore canvas to state before previous frame was drawn
                    Array.Copy(savedCanvasForDisposal3, currentCanvas, currentCanvas.Length);
                }
            }

            // 2. Save snapshot of canvas prior to drawing frame (required for Disposal 3)
            Array.Copy(currentCanvas, savedCanvasForDisposal3, currentCanvas.Length);

            // 3. Decode frame LZW data and composite onto current canvas
            byte[] decodedData = GetDecodedData(imageBlock);
            int dataIndex = 0;

            for (int y = canvasHeight - 1; y >= 0; y--)
            {
                WriteTexturePixelRow(
                    currentCanvas,
                    canvasWidth,
                    canvasHeight,
                    y,
                    imageBlock,
                    decodedData,
                    ref dataIndex,
                    colorTable,
                    bgColor,
                    transparentIndex,
                    true // filledTexture = true preserves currentCanvas where pixels are transparent
                );
            }

            float delaySec = GetDelaySec(graphicCtrlEx);

            decodedFrames.Add(new DecodedFrame
            {
                width = canvasWidth,
                height = canvasHeight,
                delaySec = delaySec,
                pixels = (Color32[])currentCanvas.Clone()
            });

            prevImageBlock = imageBlock;
            prevDisposalMethod = disposalMethod;
        }

        return decodedFrames.ToArray();
    }

    private static void ClearImageBlockRegion(Color32[] canvas, int width, int height, ImageBlock imageBlock, Color32 clearColor)
    {
        int left = imageBlock.m_imageLeftPosition;
        int top = imageBlock.m_imageTopPosition;
        int subWidth = imageBlock.m_imageWidth;
        int subHeight = imageBlock.m_imageHeight;

        for (int row = 0; row < subHeight; row++)
        {
            int gifYFromTop = top + row;
            int unityY = height - 1 - gifYFromTop;
            if (unityY < 0 || unityY >= height) continue;

            int baseIndex = unityY * width;
            for (int col = 0; col < subWidth; col++)
            {
                int unityX = left + col;
                if (unityX < 0 || unityX >= width) continue;
                canvas[baseIndex + unityX] = clearColor;
            }
        }
    }
}