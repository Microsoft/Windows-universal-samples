//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

#include "pch.h"

#include "Common\DirectXHelper.h"
#include "TextRenderer.h"

using namespace HolographicTagAlongSample;

TextRenderer::TextRenderer(const std::shared_ptr<DX::DeviceResources>& deviceResources, unsigned int const& textureWidth, unsigned int const& textureHeight)
    : m_deviceResources(deviceResources),
      m_textureWidth(textureWidth),
      m_textureHeight(textureHeight)
{
    CreateDeviceDependentResources();
}

void TextRenderer::RenderTextOffscreen(const std::wstring& str)
{
    // Clear the off-screen render target.
    m_deviceResources->GetD3DDeviceContext()->ClearRenderTargetView(m_renderTargetView.Get(), DirectX::Colors::Transparent);

    // Begin drawing with D2D.
    m_d2dRenderTarget->BeginDraw();

    // Create a text layout to match the screen.
    Microsoft::WRL::ComPtr<IDWriteTextLayout> textLayout;
    m_deviceResources->GetDWriteFactory()->CreateTextLayout(
        str.c_str(),
        static_cast<UINT32>(str.length()),
        m_textFormat.Get(),
        (float)m_textureWidth,
        (float)m_textureHeight,
        &textLayout
        );

    // Get the text metrics from the text layout.
    DWRITE_TEXT_METRICS metrics;
    DX::ThrowIfFailed(textLayout->GetMetrics(&metrics));

    // In this example, we position the text in the center of the off-screen render target.
    D2D1::Matrix3x2F screenTranslation = D2D1::Matrix3x2F::Translation(
        m_textureWidth * 0.5f,
        m_textureHeight * 0.5f + metrics.height * 0.5f
        );
    m_whiteBrush->SetTransform(screenTranslation);

    // Render the text using DirectWrite.
    m_d2dRenderTarget->DrawTextLayout(
        D2D1::Point2F(0.0f, 0.0f),
        textLayout.Get(),
        m_whiteBrush.Get()
        );

    // End drawing with D2D.
    HRESULT hr = m_d2dRenderTarget->EndDraw();
    if (hr != D2DERR_RECREATE_TARGET)
    {
        // Catch errors from D2D.
        DX::ThrowIfFailed(hr);
    }
}

void TextRenderer::ReleaseDeviceDependentResources()
{
    m_texture.Reset();
    m_shaderResourceView.Reset();
    m_pointSampler.Reset();
    m_renderTargetView.Reset();
    m_d2dRenderTarget.Reset();
    m_whiteBrush.Reset();
    m_textFormat.Reset();
}

void TextRenderer::CreateDeviceDependentResources()
{
    // Create a default sampler state, which will use point sampling.
    CD3D11_SAMPLER_DESC desc(D3D11_DEFAULT);
    m_deviceResources->GetD3DDevice()->CreateSamplerState(&desc, &m_pointSampler);

    // Create the texture that will be used as the offscreen render target.
    CD3D11_TEXTURE2D_DESC textureDesc(
        DXGI_FORMAT_B8G8R8A8_UNORM,
        m_textureWidth,
        m_textureHeight,
        1,
        1,
        D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET
    );
    m_deviceResources->GetD3DDevice()->CreateTexture2D(&textureDesc, nullptr, &m_texture);

    // Create read and write views for the offscreen render target.
    m_deviceResources->GetD3DDevice()->CreateShaderResourceView(m_texture.Get(), nullptr, &m_shaderResourceView);
    m_deviceResources->GetD3DDevice()->CreateRenderTargetView(m_texture.Get(), nullptr, &m_renderTargetView);

    // In this example, we are using D2D and DirectWrite; so, we need to create a D2D render target as well.
    D2D1_RENDER_TARGET_PROPERTIES props = D2D1::RenderTargetProperties(
        D2D1_RENDER_TARGET_TYPE_DEFAULT,
        D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED),
        96,
        96
    );

    // The DXGI surface is used to create the render target.
    Microsoft::WRL::ComPtr<IDXGISurface> dxgiSurface;
    DX::ThrowIfFailed(m_texture.As(&dxgiSurface));
    DX::ThrowIfFailed(m_deviceResources->GetD2DFactory()->CreateDxgiSurfaceRenderTarget(dxgiSurface.Get(), &props, &m_d2dRenderTarget));

    // Create a solid color brush that will be used to render the text.
    DX::ThrowIfFailed(m_d2dRenderTarget->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::Cornsilk), &m_whiteBrush));

    // This is where we format the text that will be written on the render target.
    DX::ThrowIfFailed(
        m_deviceResources->GetDWriteFactory()->CreateTextFormat(
            L"Segoe UI",
            NULL,
            DWRITE_FONT_WEIGHT_NORMAL,
            DWRITE_FONT_STYLE_NORMAL,
            DWRITE_FONT_STRETCH_NORMAL,
            400.0f,
            L"",
            m_textFormat.ReleaseAndGetAddressOf()
        )
    );
    DX::ThrowIfFailed(m_textFormat->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_CENTER));
    DX::ThrowIfFailed(m_textFormat->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_CENTER));
}

