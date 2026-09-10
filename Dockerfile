# ============================================================
# 1. BUILD APPLICATION
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build

WORKDIR /src

COPY . .

RUN dotnet restore \
    "src/TheIgKnightWorker.Host/TheIgKnightWorker.csproj"

RUN dotnet publish \
    "src/TheIgKnightWorker.Host/TheIgKnightWorker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:DebugType=None \
    /p:DebugSymbols=false


# ============================================================
# 2. PLAYWRIGHT / CHROMIUM PREPARATION
#
# This stage is ONLY used to obtain the exact Chromium
# headless-shell required by Playwright 1.62.0.
#
# The Playwright base image itself is NEVER used in production.
# ============================================================
FROM mcr.microsoft.com/playwright/dotnet:v1.62.0-noble AS browser

WORKDIR /app

COPY --from=build /app/publish .

# Install ONLY Chromium headless shell.
RUN pwsh /app/playwright.ps1 install --only-shell

# Copy ONLY the Chromium headless-shell browser directory
# to a clean location.
RUN mkdir -p /browser && \
    cp -a /ms-playwright/chromium_headless_shell-* /browser/


# ============================================================
# 3. PRODUCTION RUNTIME
# ============================================================
FROM mcr.microsoft.com/dotnet/runtime:9.0-noble AS final

WORKDIR /app

ENV \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright


# ============================================================
# 4. CHROMIUM NATIVE DEPENDENCIES
#
# These are the native libraries required by Chromium
# headless-shell on Ubuntu Noble.
# ============================================================
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        fonts-liberation \
        fonts-noto-color-emoji \
        fontconfig \
        libasound2t64 \
        libatk-bridge2.0-0t64 \
        libatk1.0-0t64 \
        libatspi2.0-0t64 \
        libcairo2 \
        libcups2t64 \
        libdbus-1-3 \
        libdrm2 \
        libgbm1 \
        libglib2.0-0t64 \
        libnspr4 \
        libnss3 \
        libpango-1.0-0 \
        libx11-6 \
        libx11-xcb1 \
        libxcb1 \
        libxcomposite1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxkbcommon0 \
        libxrandr2 \
        libxshmfence1 \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /usr/share/doc/* \
    && rm -rf /usr/share/man/* \
    && rm -rf /usr/share/info/*


# ============================================================
# 5. APPLICATION
# ============================================================
COPY --from=build /app/publish .


# ============================================================
# 6. REMOVE NON-PRODUCTION .NET FILES
# ============================================================

# Debug symbols are not needed in the production container.
RUN find /app \
    -type f \
    \( -name "*.pdb" -o -name "*.xml" \) \
    -delete


# ============================================================
# 7. REMOVE UNUSED .NET LOCALIZATION RESOURCES
#
# Keep the application itself intact.
# These are framework satellite resources that are not required
# for the Worker PDF-generation runtime.
# ============================================================
RUN find /app \
    -mindepth 1 \
    -maxdepth 1 \
    -type d \
    \( \
        -name "cs" \
        -o -name "de" \
        -o -name "es" \
        -o -name "fr" \
        -o -name "it" \
        -o -name "ja" \
        -o -name "ko" \
        -o -name "pl" \
        -o -name "pt-BR" \
        -o -name "ru" \
        -o -name "tr" \
        -o -name "zh-Hans" \
        -o -name "zh-Hant" \
    \) \
    -exec rm -rf {} +


# ============================================================
# 8. CHROMIUM HEADLESS SHELL ONLY
# ============================================================
COPY --from=browser \
    /browser/ \
    /ms-playwright/


# ============================================================
# 9. REMOVE UNNECESSARY CHROMIUM DATA
#
# Resume generation does not require Chromium UI translation
# resources or hyphenation dictionaries.
# ============================================================
RUN find /ms-playwright \
    -type d \
    -name "locales" \
    -exec rm -rf {} + \
    && find /ms-playwright \
    -type d \
    -name "hyphen-data" \
    -exec rm -rf {} +


# ============================================================
# 10. FINAL CLEANUP
# ============================================================
RUN rm -rf \
    /tmp/* \
    /var/tmp/*


# ============================================================
# 11. START WORKER
# ============================================================
ENTRYPOINT ["dotnet", "TheIgKnightWorker.dll"]