# Promoted Final rainCombined — Live Frame Debugger Measurement

The approved live Unity Frame Debugger capture of the promoted Build C architecture measured the following for both PL2 and PL3 rain at the visible phase:

| Metric | Value |
|---|---:|
| Render events | 1 (`Draw Dynamic rainCombined`) |
| Rain Draw Calls | 1 |
| Vertices | 16 |
| Indices | 24 |
| Triangles | 8 |
| Shader | `Mobile/Particles/Additive` |
| Blend | `SrcAlpha / One` |
| ZWrite | Off |

These values are **MEASURED** live Frame Debugger evidence supplied for the promotion decision. The automated Editor capture path could not export the live Frame Debugger window, so this repository evidence record contains the measured values while the promoted-final visual output is preserved separately in `after_pl2_rainCombined_0.20s.png` and `after_pl3_rainCombined_0.20s.png`.

The same promoted-final live Game View run measured whole-variant counters of **8 Draw Calls / 8 Batches / 8 SetPass Calls for PL2** and **8 / 8 / 7 for PL3**. Those counters are stored in `final_live_render_validation.json`.
