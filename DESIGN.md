\# Design System Document: Technical Precision \& Editorial Depth

&#x20;

\## 1. Overview \& Creative North Star: "The Architectural Lens"

In the world of BIM and Revit, data is often cluttered and overwhelming. This design system moves beyond the "utility-only" mindset of CAD plugins to provide an experience of \*\*Architectural Clarity\*\*. Our Creative North Star is \*\*The Architectural Lens\*\*: a philosophy that treats digital interfaces like architectural plans—precise, layered, and intentionally spaced.

&#x20;

To break the "standard plugin" look, we employ \*\*Intentional Asymmetry\*\* and \*\*Tonal Depth\*\*. Instead of a rigid grid of boxes, we use a hierarchy of overlapping surfaces and refined typography to guide the user’s eye through complex export workflows. The goal is an interface that feels like a high-end technical journal—authoritative, dense with information, yet incredibly easy to breathe in.

&#x20;

\---

&#x20;

\### 2. Colors \& Surface Philosophy

The palette is rooted in professional blues and slate grays, but its execution is what defines its premium feel.

&#x20;

\#### The "No-Line" Rule

Traditional UI relies on 1px borders to separate sections. In this design system, \*\*lines are prohibited for sectioning.\*\* Boundaries must be defined through background color shifts.

\- Use `surface\_container\_low` for the main workspace.

\- Use `surface\_container\_highest` for sidebars or utility panels.

\- This creates a seamless, modern look that mimics the way light falls on different planes in a 3D model.

&#x20;

\#### Surface Hierarchy \& Nesting

Treat the UI as a series of physical layers. Use the `surface\_container` tiers to create "nested" depth:

\- \*\*Base Layer:\*\* `surface` (#f8f9ff)

\- \*\*Primary Sectioning:\*\* `surface\_container\_low` (#eff4ff)

\- \*\*Nested Components (Cards/Modals):\*\* `surface\_container\_lowest` (#ffffff) to create a subtle "lift."

&#x20;

\#### The "Glass \& Gradient" Rule

To add a "soul" to the technical environment, main CTAs and progress indicators should utilize a subtle linear gradient transitioning from `primary` (#00488d) to `primary\_container` (#005fb8). For floating windows or tool palettes, use \*\*Glassmorphism\*\*: 

\- Background: `surface\_container\_low` at 80% opacity.

\- Effect: `backdrop-filter: blur(12px)`.

&#x20;

\---

&#x20;

\### 3. Typography: Editorial Technicality

We pair \*\*Manrope\*\* for high-level display and \*\*Inter\*\* for technical data. This juxtaposition creates an "Editorial Technical" vibe—where the software feels both cutting-edge and deeply reliable.

&#x20;

\*   \*\*Display \& Headlines (Manrope):\*\* Used for page titles and major section headers. The wider aperture of Manrope adds a modern, architectural feel. 

&#x20;   \*   \*Headline-lg:\* 2rem — Reserved for the primary dashboard state.

\*   \*\*Titles \& Body (Inter):\*\* Used for all functional data. Inter’s high x-height ensures readability when users are scanning long lists of Revit families or parameters.

&#x20;   \*   \*Body-md:\* 0.875rem — The workhorse for list items and descriptions.

\*   \*\*Labels (Inter):\*\* 

&#x20;   \*   \*Label-sm:\* 0.6875rem — Used for metadata and overlines. Always uppercase with a +5% letter-spacing to maintain a "blueprint" aesthetic.

&#x20;

\---

&#x20;

\### 4. Elevation \& Depth: Tonal Layering

We avoid the "pasted-on" look of traditional shadows in favor of natural ambient light.

&#x20;

\*   \*\*The Layering Principle:\*\* Depth is achieved by stacking. Place a `surface\_container\_lowest` card on a `surface\_container\_low` background. This creates a "soft lift" that feels integrated.

\*   \*\*Ambient Shadows:\*\* For floating elements (like dropdowns), use extra-diffused shadows.

&#x20;   \*   \*Shadow:\* `0px 12px 32px rgba(11, 28, 48, 0.06)` — Note the use of `on\_surface` (#0b1c30) as the shadow tint rather than pure black.

\*   \*\*The "Ghost Border" Fallback:\*\* If a border is required for accessibility, use the `outline\_variant` (#c2c6d4) at 20% opacity. Never use 100% opaque borders.

&#x20;

\---

&#x20;

\### 5. Components

&#x20;

\#### Buttons

\- \*\*Primary:\*\* Gradient fill (`primary` to `primary\_container`), `roundness-md` (0.375rem). White text.

\- \*\*Secondary:\*\* Transparent background with a `ghost-border` (20% `outline\_variant`). 

\- \*\*Tertiary:\*\* Text-only using `primary` color. High-density padding: `8px 16px`.

&#x20;

\#### Input Fields

\- \*\*Style:\*\* Instead of a full box, use a `surface\_container\_high` background with a `surface\_tint` bottom-only highlight (2px) on focus. This mimics professional drafting software.

\- \*\*Error States:\*\* Use `error` (#ba1a1a) for the highlight and `on\_error\_container` for the helper text.

&#x20;

\#### Cards \& Lists

\- \*\*Rule:\*\* \*\*No divider lines.\*\*

\- Separate Revit items using 12px of vertical white space or a subtle hover state shift to `surface\_container\_highest`.

\- Use `roundness-lg` (0.5rem) for cards to soften the technical density.

&#x20;

\#### Progress Indicators

\- For Revit exports, use a thick 8px linear bar. 

\- Background: `surface\_container\_highest`. 

\- Fill: A gradient of `primary` to `primary\_container`.

\- \*\*Micro-interaction:\*\* Include a subtle pulse animation on the leading edge of the progress bar to indicate active processing.

&#x20;

\#### Export Chips

\- Use `secondary\_container` (#d5e3fc) for file format tags (e.g., .IFC, .DWG). 

\- Use `tertiary\_container` (#a04401) for "High Priority" or "Warning" tags to ensure they pop against the blue/slate background.

&#x20;

\---

&#x20;

\### 6. Do’s and Don’ts

&#x20;

\#### Do

\- \*\*Do\*\* use `surface\_bright` to highlight the "Active" state of a panel.

\- \*\*Do\*\* use high-contrast `display` typography to create clear entry points in the UI.

\- \*\*Do\*\* embrace white space. Information density does not mean lack of margins.

&#x20;

\#### Don’t

\- \*\*Don't\*\* use 1px solid black or high-contrast borders. It breaks the "Architectural Lens" immersion.

\- \*\*Don't\*\* use standard "Drop Shadows." Stick to tonal shifts and ambient, tinted blurs.

\- \*\*Don't\*\* mix more than two levels of `surface\_container` in a single view to avoid visual "noise."

