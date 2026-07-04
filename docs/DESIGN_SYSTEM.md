# BikeMate Design System

BikeMate uses a conservative service-app visual system across WebAdmin, customer mobile, and shop mobile.

## Color Tokens

| Token | Value | Use |
| --- | --- | --- |
| Brand orange | `#FF6B00` | Primary actions, active states, brand accents |
| Brand orange dark | `#C45500` | Hover states, pressed states, high-contrast accents |
| Navy | `#22303D` | Admin navigation, dark headers, strong surfaces |
| Text | `#242424` | Main body and heading text |
| Muted text | `#6E6E6E` | Secondary copy, metadata, helper text |
| Page background | `#F6F7F9` | App and admin page backgrounds |
| Card background | `#FFFFFF` | Cards, modals, framed tools |
| Border | `#E1E5EA` | Dividers, inputs, cards |
| Soft orange | `#FFF3EA` | Light primary highlights and selected states |
| Success | `#16A34A` | Positive status |
| Warning | `#CA8A04` | Pending or attention status |
| Error | `#DC2626` | Failed, rejected, destructive states |

## Typography

Use the same three-size scale everywhere:

| Token | Size | Font | Use |
| --- | --- | --- | --- |
| Caption | `11` | PT Sans Caption | Labels, chips, metadata |
| Body | `13` | Public Sans | Normal app copy and inputs |
| Title | `18` | Inter | Page titles, card titles, important numbers |

Keep letter spacing at `0`. Avoid viewport-scaled type and one-off font sizes unless the page is a true hero or a media surface.

## Components

Use 8px radius for compact buttons and inputs, and 8-12px radius for cards depending on platform conventions. Prefer tokenized colors and styles over hard-coded page values.
