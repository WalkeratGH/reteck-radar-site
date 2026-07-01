# 01 · Requirements Specification

> 中文版：`01-開發需求.md`
> This is the system's original requirements (initial concept). For implementation
> status see `02-Development-Log-and-Decisions.md`.

## Purpose

Help search, organize, and evaluate global IT System Integrators, Data Center Service
Vendors, and IT service providers with small-scale AI-compute build capability — as a
partner database for Re-Teck across Microsoft, Dell, Nokia, Data Center, ITAD,
AI Server / Edge AI / GPU Workstation projects.

**Current scope: US-based providers first.**

## Technology platform

- .NET 8 (or latest stable), C#
- SQLite + Entity Framework Core
- ASP.NET Core MVC (or Blazor Server)
- Bootstrap UI
- CSV / Excel export
- Local MVP first; **do not integrate paid APIs at the start**

## Phase-1 MVP features

### 1. Partner search & record-keeping
Manually add / edit / delete / search provider records. Classification focus:
IT System Integrator, Data Center Smart Hands, IMAC Service Provider, Break/Fix IT
Service, Network/Server/Storage Support, Microsoft/Dell/Cisco/HPE Partner, IT Field
Service Provider, AI Server Builder, GPU Workstation Builder, Edge AI Solution Provider,
Local AI Infrastructure Provider, Small AI Computing Deployment Partner.

### 2. Data fields

**Basic:** Company Name, Country, City, Website, Service Category, Main Services,
Contact Person, Contact Title, Email, Phone, LinkedIn, Source URL, Notes, Last Updated Date.

**IT / Data Center capability:** Data Center Experience, Smart Hands, IMAC, Break/Fix,
Network/Server/Storage Support, Microsoft/Dell/Cisco/HPE Partner Status, Certifications
(e.g. ISO 9001 / ISO 27001).

**AI Infrastructure capability:** AI Server Build, GPU Workstation Build, Edge AI
Deployment, Local LLM Deployment, NVIDIA GPU, AMD GPU, NVIDIA Jetson / Edge Device,
Small AI Cluster, On-Prem AI Deployment, AI Model Inference Environment Setup,
Linux/Docker/Kubernetes, Cooling/Power Planning, AI Infrastructure Summary.

**AI / evaluation:** AI Summary, Qualification Score, AI Infrastructure Score,
Recommended Level (A/B/C), Manual Review Status, Follow Up Action.

### 3. Partner Qualification Scoring Model (100 pts)

**General IT / SI capability (70)**
- Data Center / Enterprise IT experience: 20
- Local coverage capability: 10
- Brand partnership (Microsoft/Dell/Cisco/HPE): 10
- Certification and compliance: 10
- Service scope match: 10
- Contact availability: 5
- Website / public information credibility: 5

**AI Infrastructure capability (30)**
- AI server / GPU workstation deployment experience: 10
- Edge AI / local AI deployment experience: 8
- Linux / Docker / Kubernetes capability: 6
- Cooling / power planning for small AI computing: 4
- NVIDIA / AMD GPU ecosystem experience: 2

**Grading:** A = 80–100 (contact first), B = 60–79 (confirm further),
C = below 60 (keep on hold).

### 4. Search feature
Build a "Search Keyword Generator" first (**no Google API needed initially**). User
enters Country / City / Service Type / AI Capability Required (Yes/No); the system
produces search phrases (e.g. "GPU workstation provider São Paulo"), copyable, or the
user pastes results and files candidate companies manually.

### 5. Reserved for future expansion
Web Search API connector, SerpAPI connector, Microsoft Partner directory connector,
LinkedIn manual research workflow, AI Summary generator, Automatic scoring engine,
Weekly market radar, Duplicate company detection.

### 6. UI pages
Dashboard, Partner List, Add/Edit Partner, Partner Detail, Search Keyword Generator,
Qualification Score View, Export CSV/Excel, Settings.

### 7. Dashboard shows
Total Partners, A/B/C level count, Partners by Country, AI-Infrastructure-capable count,
Partners missing contact information, Partners pending manual review.

### 8. Deliverables
System architecture, database schema, entity models, DbContext, migration / SQLite init,
basic CRUD, Search Keyword Generator, Scoring Service, Bootstrap UI, first runnable code.
Keep the code simple, maintainable by non-IT staff.
