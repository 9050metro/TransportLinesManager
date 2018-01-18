﻿using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Extensions;
using Klyte.TransportLinesManager.Extensors.TransportLineExt;
using Klyte.TransportLinesManager.Extensors.TransportTypeExt;
using Klyte.TransportLinesManager.UI;
using Klyte.TransportLinesManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Klyte.TransportLinesManager.LineList.ExtraUI
{
    public class TLMAssetSelectorWindow
    {
        private UIPanel m_parent => m_lineInfo.mainPanel;
        private UIPanel m_mainPanel;
        private UIHelperExtension m_uiHelper;
        private UILabel m_title;
        private TLMLineInfoPanel m_lineInfo;

        private UIScrollablePanel m_scrollablePanel;
        private UIScrollbar m_scrollbar;
        private AVOPreviewRenderer m_previewRenderer;
        private UITextureSprite m_preview;
        private UIPanel m_previewPanel;
        private VehicleInfo m_lastInfo;
        private Dictionary<string, string> m_defaultAssets = new Dictionary<string, string>();
        private Dictionary<string, UICheckBox> m_checkboxes = new Dictionary<string, UICheckBox>();
        private TransportSystemDefinition m_lastDef = null;
        private bool m_isLoading;

        public TLMAssetSelectorWindow(TLMLineInfoPanel parent)
        {
            m_lineInfo = parent;
            CreateWindow();
        }

        private void CreateWindow()
        {
            CreateMainPanel();

            CreateScrollPanel();

            SetPreviewWindow();

            BindParentChanges();
        }

        private void CreateMainPanel()
        {
            TLMUtils.createUIElement(ref m_mainPanel, m_parent.transform);
            m_mainPanel.Hide();
            m_mainPanel.relativePosition = new Vector3(m_parent.width, 0.0f);
            m_mainPanel.width = 250;
            m_mainPanel.height = m_parent.height;
            m_mainPanel.zOrder = 50;
            m_mainPanel.color = new Color32(255, 255, 255, 255);
            m_mainPanel.backgroundSprite = "MenuPanel2";
            m_mainPanel.name = "AssetSelectorWindow";
            m_mainPanel.autoLayoutPadding = new RectOffset(5, 5, 10, 10);
            m_mainPanel.autoLayout = false;
            m_mainPanel.useCenter = true;
            m_mainPanel.wrapLayout = false;
            m_mainPanel.canFocus = true;
            TLMUtils.createDragHandle(m_mainPanel, m_mainPanel, 35f);
            m_parent.eventVisibilityChanged += (component, value) =>
            {
                m_mainPanel.isVisible = value;
            };

            TLMUtils.createUIElement(ref m_title, m_mainPanel.transform);
            m_title.textAlignment = UIHorizontalAlignment.Center;
            m_title.autoSize = false;
            m_title.autoHeight = true;
            m_title.width = m_mainPanel.width - 10f;
            m_title.relativePosition = new Vector3(5, 5);
            m_title.textScale = 0.9f;
        }

        private void CreateScrollPanel()
        {
            TLMUtils.createUIElement(ref m_scrollablePanel, m_mainPanel.transform);
            m_scrollablePanel.width = m_mainPanel.width - 20f;
            m_scrollablePanel.height = m_mainPanel.height - 50f;
            m_scrollablePanel.autoLayoutDirection = LayoutDirection.Vertical;
            m_scrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            m_scrollablePanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            m_scrollablePanel.autoLayout = true;
            m_scrollablePanel.clipChildren = true;
            m_scrollablePanel.relativePosition = new Vector3(5, 45);

            UIPanel trackballPanel = null;
            TLMUtils.createUIElement(ref trackballPanel, m_mainPanel.transform);
            trackballPanel.width = 10f;
            trackballPanel.height = m_scrollablePanel.height;
            trackballPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            trackballPanel.autoLayoutStart = LayoutStart.TopLeft;
            trackballPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            trackballPanel.autoLayout = true;
            trackballPanel.relativePosition = new Vector3(m_mainPanel.width - 15, 45);


            TLMUtils.createUIElement(ref m_scrollbar, trackballPanel.transform);
            m_scrollbar.width = 10f;
            m_scrollbar.height = m_scrollbar.parent.height;
            m_scrollbar.orientation = UIOrientation.Vertical;
            m_scrollbar.pivot = UIPivotPoint.BottomLeft;
            m_scrollbar.AlignTo(trackballPanel, UIAlignAnchor.TopRight);
            m_scrollbar.minValue = 0f;
            m_scrollbar.value = 0f;
            m_scrollbar.incrementAmount = 25f;

            UISlicedSprite scrollBg = null;
            TLMUtils.createUIElement(ref scrollBg, m_scrollbar.transform);
            scrollBg.relativePosition = Vector2.zero;
            scrollBg.autoSize = true;
            scrollBg.size = scrollBg.parent.size;
            scrollBg.fillDirection = UIFillDirection.Vertical;
            scrollBg.spriteName = "ScrollbarTrack";
            m_scrollbar.trackObject = scrollBg;

            UISlicedSprite scrollFg = null;
            TLMUtils.createUIElement(ref scrollFg, scrollBg.transform);
            scrollFg.relativePosition = Vector2.zero;
            scrollFg.fillDirection = UIFillDirection.Vertical;
            scrollFg.autoSize = true;
            scrollFg.width = scrollFg.parent.width - 4f;
            scrollFg.spriteName = "ScrollbarThumb";
            m_scrollbar.thumbObject = scrollFg;
            m_scrollablePanel.verticalScrollbar = m_scrollbar;
            m_scrollablePanel.eventMouseWheel += delegate (UIComponent component, UIMouseEventParameter param)
            {
                m_scrollablePanel.scrollPosition += new Vector2(0f, Mathf.Sign(param.wheelDelta) * -1f * m_scrollbar.incrementAmount);
            };
            m_scrollablePanel.eventMouseLeave += (x, y) =>
            {
                m_previewPanel.isVisible = false;
            };

            m_uiHelper = new UIHelperExtension(m_scrollablePanel);
        }

        private void BindParentChanges()
        {
            m_lineInfo.EventOnLineChanged += (lineId) =>
            {
                TLMUtils.doLog("EventOnLineChanged");
                m_isLoading = true;
                bool isCustomLine = TLMTransportLineExtensions.instance.GetUseCustomConfig(lineId);
                TransportSystemDefinition tsd = TransportSystemDefinition.from(lineId);
                TLMUtils.doLog("tsd = {0}", tsd);
                if (m_lastDef != tsd)
                {
                    foreach (var i in m_checkboxes.Keys)
                    {
                        UnityEngine.Object.Destroy(m_checkboxes[i].gameObject);
                    }
                    m_defaultAssets = tsd.GetTransportExtension().GetAllBasicAssets(0);
                    m_checkboxes = new Dictionary<string, UICheckBox>();

                    TLMUtils.doLog("m_defaultAssets Size = {0} ({1})", m_defaultAssets?.Count, string.Join(",", m_defaultAssets.Keys?.ToArray() ?? new string[0]));
                    foreach (var i in m_defaultAssets.Keys)
                    {
                        var checkbox = (UICheckBox)m_uiHelper.AddCheckbox(m_defaultAssets[i], false, (x) =>
                        {
                            ushort lineIdx = m_lineInfo.CurrentSelectedId;
                            if (m_isLoading) return;
                            if (x)
                            {
                                if (TLMTransportLineExtensions.instance.GetUseCustomConfig(lineIdx))
                                {
                                    TLMTransportLineExtensions.instance.AddAsset(lineIdx, i);
                                }
                                else
                                {
                                    tsd.GetTransportExtension().AddAsset(TLMLineUtils.getPrefix(lineIdx), i);
                                }
                            }
                            else
                            {
                                if (TLMTransportLineExtensions.instance.GetUseCustomConfig(lineIdx))
                                {
                                    TLMTransportLineExtensions.instance.RemoveAsset(lineIdx, i);
                                }
                                else
                                {
                                    tsd.GetTransportExtension().RemoveAsset(TLMLineUtils.getPrefix(lineIdx), i);
                                }
                            }
                        });
                        CreateModelCheckBox(i, checkbox);
                        checkbox.label.tooltip = checkbox.label.text;
                        checkbox.label.textScale = 0.9f;
                        checkbox.label.transform.localScale = new Vector3(Math.Min((m_mainPanel.width - 50) / checkbox.label.width, 1), 1);
                        m_checkboxes[i] = checkbox;
                    }
                }
                m_lastDef = tsd;
                List<string> selectedAssets;
                if (isCustomLine)
                {
                    selectedAssets = TLMTransportLineExtensions.instance.GetAssetList(lineId);
                }
                else
                {
                    selectedAssets = tsd.GetTransportExtension().GetAssetList(TLMLineUtils.getPrefix(lineId));
                }
                TLMUtils.doLog("selectedAssets Size = {0} ({1})", selectedAssets?.Count, string.Join(",", selectedAssets?.ToArray() ?? new string[0]));
                foreach (var i in m_checkboxes.Keys)
                {
                    m_checkboxes[i].isChecked = selectedAssets.Contains(i);
                }

                if (isCustomLine)
                {
                    m_title.text = string.Format(Locale.Get("TLM_ASSET_SELECT_WINDOW_TITLE"), TLMLineUtils.getLineStringId(lineId), TLMConfigWarehouse.getNameForTransportType(tsd.toConfigIndex()));
                }
                else
                {
                    TLMConfigWarehouse.ConfigIndex transportType = tsd.toConfigIndex();
                    ModoNomenclatura mnPrefixo = (ModoNomenclatura)TLMConfigWarehouse.getCurrentConfigInt(TLMConfigWarehouse.ConfigIndex.PREFIX | transportType);
                    var prefix = TLMLineUtils.getPrefix(lineId);
                    m_title.text = string.Format(Locale.Get("TLM_ASSET_SELECT_WINDOW_TITLE_PREFIX"), prefix > 0 ? TLMUtils.getStringFromNumber(TLMUtils.getStringOptionsForPrefix(mnPrefixo), (int)prefix + 1) : Locale.Get("TLM_UNPREFIXED"), TLMConfigWarehouse.getNameForTransportType(tsd.toConfigIndex()));
                }

                m_isLoading = false;
                m_previewPanel.isVisible = false;
            };
        }
        

        private void CreateModelCheckBox(string i, UICheckBox checkbox)
        {
            checkbox.eventMouseEnter += (x, y) =>
            {
                m_lastInfo = PrefabCollection<VehicleInfo>.FindLoaded(i);
                redrawModel();
            };

        }

        private void SetPreviewWindow()
        {
            TLMUtils.createUIElement(ref m_previewPanel, m_mainPanel.transform);
            m_previewPanel.backgroundSprite = "GenericPanel";
            m_previewPanel.width = m_mainPanel.width + 100f;
            m_previewPanel.height = m_mainPanel.width;
            m_previewPanel.relativePosition = new Vector3(-50f, m_mainPanel.height);
            TLMUtils.createUIElement(ref m_preview, m_previewPanel.transform);
            this.m_preview.size = m_previewPanel.size;
            this.m_preview.relativePosition = Vector3.zero;
            TLMUtils.createUIElement(ref m_previewRenderer, m_mainPanel.transform);
            this.m_previewRenderer.size = this.m_preview.size * 2f;
            this.m_preview.texture = this.m_previewRenderer.texture;
            m_previewRenderer.zoom = 3;
            m_previewRenderer.cameraRotation = 40;
            m_previewPanel.isVisible = false;
        }

        public void RotateCamera()
        {
            if (m_lastInfo != default(VehicleInfo) && m_previewPanel.isVisible)
            {
                this.m_previewRenderer.cameraRotation -= 2;
                redrawModel();
            }
        }

        private void redrawModel()
        {
            if (m_lastInfo == default(VehicleInfo))
            {
                m_previewPanel.isVisible = false;
                return;
            }
            m_previewPanel.isVisible = true;
            m_previewRenderer.RenderVehicle(m_lastInfo, Color.HSVToRGB(Math.Abs(m_previewRenderer.cameraRotation) / 360f, .5f, .5f), true);
        }
    }
}