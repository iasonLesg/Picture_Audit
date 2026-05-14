# 🔍 Picture_Audit

**Picture_Audit** is a high-performance forensic analysis desktop application built with C# and WPF. It allows investigators and enthusiasts to perform deep image inspection locally, ensuring total privacy and data security.

---

## 🛠 Forensic Analysis Suite
The application leverages multiple mathematical and statistical algorithms to detect image manipulation and discrepancies:

*   **Block Decomposition Analysis:** Detects inconsistencies in image block structures.
*   **CAGI Analysis:** Content-Aware Gradient Inspection to find structural anomalies.
*   **Double Quantization (DQ):** Identifies if an image has been re-saved or tampered with at the JPEG level.
*   **Error Level Analysis (ELA):** Highlights areas with different compression levels.
*   **Ghost Image Analysis:** Uncovers traces of moved or deleted elements.
*   **Hidden Metadata:** Extracts EXIF, XMP, and IPTC data, including GPS and camera serials.
*   **Median Analysis:** Detects filtering and smoothing artifacts.
*   **Wavelet Analysis:** Breaks down frequency components to find noise patterns indicative of editing.

---

## ✨ Key Features
*   **Local Processing:** No data ever leaves your machine. Your images stay private.
*   **Annotation Engine:** Draw, highlight, and label discrepancies directly on the forensic results.
*   **Metadata Extraction:** Deep dive into the "digital fingerprint" of every file.
*   **WPF Interface:** A clean, hardware-accelerated Windows UI for smooth image manipulation.

---

## 🚀 Getting Started

### Prerequisites
*   .NET 6.0 / .NET 8.0 SDK (depending on your project version)
*   Windows 10/11 (for WPF support)

### Installation
1. Clone the repository:
   ```bash
   git clone [https://github.com/iasonLesg/Picture_Audit.git](https://github.com/iasonLesg/Picture_Audit.git)


2.Open Picture_Audit.sln in Visual Studio.

3.Restore NuGet packages.

4.Build and Run!