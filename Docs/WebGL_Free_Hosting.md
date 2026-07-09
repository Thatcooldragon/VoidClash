# VoidClash WebGL Free Hosting

## Current Result

- Browser build folder: `BuildWebGL/`
- Upload-ready archive: `VoidClash-v0.16.0-webgl.zip`
- Build target: Unity WebGL, static-host friendly, with compression disabled.
- Touch shortcut: two-finger tap sends the same command as right-click.

## Fastest Free Play Link: itch.io

1. Create or open an itch.io project.
2. Set **Kind of project** to **HTML**.
3. Upload `VoidClash-v0.16.0-webgl.zip`.
4. Tick **This file will be played in the browser**.
5. Save the page and open the itch link on iPad, Android, or Meta Quest Browser.

This is the fastest path because itch accepts the Unity WebGL zip directly.

## Free Public Website: GitHub Pages

1. Create a free GitHub repo for the playable web build, or use a `gh-pages` branch.
2. Upload the contents of `BuildWebGL/`, not the folder itself.
3. Make sure `index.html`, `Build/`, and `TemplateData/` are at the Pages root.
4. In GitHub: **Settings -> Pages -> Deploy from branch**.
5. Open the Pages URL from iPad, Android, or Quest.

The build is uncompressed so GitHub Pages does not need custom gzip or Brotli headers.

## Emergency Same-Wi-Fi Option

If there is no time to publish:

1. Start a small local web server from the `BuildWebGL/` folder.
2. Open the PC's local network address from the iPad/phone/Quest browser.
3. Keep the PC awake while playing.

This does not help while away from home, but it is useful for quick testing before uploading.

## Device Notes

- iPad and Android: tap to select, two-finger tap to issue move/attack/harvest/context commands.
- Meta Quest 3: use the browser. A Bluetooth mouse/controller pointer will be better than touch-like controls.
- WebGL performance will be lower than the Windows build, so early play should use shorter sessions and lower browser tab pressure.
