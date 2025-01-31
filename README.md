[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Github All Releases](https://img.shields.io/github/downloads/Danily07/Translumo/total.svg)]()
<a href="https://trello.com/b/MEHnLySw/translumo"><img src="https://img.shields.io/badge/Trello-0052CC?style=for-the-badge&logo=trello&logoColor=white"></a>
<p align="center">
  <img width="670" src="https://github.com/Danily07/Translumo/assets/29047281/8985049f-ea1c-428e-94be-042ece66cb54">
</p>
<!-- <p align="center"><b>EN</b> | <a href="docs/README-RU.md"><b>RU</b></a></p> -->
<p align="center">Advanced screen translator. <b>Translumo</b> is able to detect and translate appearing in the selected area text in real-time (e.g. subtitles).</p>
<p align="center">Fork from <a href="https://github.com/Danily07/Translumo">Danily07</a> - the main purpose is to make this work locally and better optimized for playing games/visual novels</p>
<h1>Main features</h1>
<ul>
  <li><b>High text recognition precision</b></li>
  <p>Translumo allows to combine the usage of several OCR engines simultaneously. It uses machine learning training model for scoring each recognized result by OCR and chooses the best one.</p>
  <p align="center">
    <img width="740" src="https://github.com/Danily07/Translumo/assets/29047281/649e5fab-a5de-4c54-a3d8-f7ea95b8f218">
  </p>
  <li><b>Simple interface</b></li>
  The main idea was to make tool, that does not require manual adjustments for each case and convenient for everyday use. 
  <li><b>Low latency</b></li>
  There are several implemented optimizations to reduce impact on system performance and minimize latency between the moment a text appears and actual translation. 
  <li><b>Integrated modern OCR engines:</b> Tesseract 5.2, WindowsOCR, EasyOCR</li>
  <li><b>Available translators:</b> Google Translate, Yandex Translate, DeepL, Locally run <a href="https://github.com/LibreTranslate/LibreTranslate" target="_blank">LibreTranslate</a> (Papago is removed as it's now a paid service from ncloud)</li>
  <li><b>Available recognition languages:</b> English, Russian, Japanese, Chinese (simplified), Korean</li>
  <li><b>Available translation languages:</b> English, Russian, Japanese, Chinese (simplified), Korean, French, Spanish, German, Portuguese, Italian, Vietnamese, Thai, Turkish, Arabic</li>
</ul>
<h1>Differences from <a href="https://github.com/Danily07/Translumo">the original</a></h1>
<ul>
<li>Added a way to manually translate by a button press while using the selected translation area</li>
<li>Removed the defunct Papago option</li>
<li>Added a way to interface with a locally running server assuming the API schema of <a href="https://github.com/LibreTranslate/LibreTranslate" target="_blank">LibreTranslate</a>. But it can be any server. I personally use a flask server (<a href="https://github.com/cthedark/flask-decoder-encoder-model-translate" target="_blank">example code</a>) </li>
<li>Added an option to record all translations and reuse them without hitting the service again. This means one can also edit the translated strings to be used later by a different user, or we can also turn these into training data.</li>
</ul>
<h1>Planned Features</a></h1>
<ul>
<li>Add back Papago option using their new paid cloud service (API key is needed).</li>
<li>More UX optimization around running games.</li>
<li>Korean Translation</li>
</ul>
<h1>System requirements</h1>
<ul>
  <li>Windows 10 build 19041 (20H1) / Windows 11</li>
  <li>DirectX11</li>
  <li>8 GB RAM <i>(for mode with EasyOCR)</i></li>
  <li>5 GB free storage space <i>(for mode with EasyOCR)</i></li>
  <li>Nvidia GPU with CUDA SDK 11.8 support (GTX 750, 8xxM, 9xx series or later) <i>(for mode with EasyOCR)</i></li>
</ul>
<h1>Demonstration</h1>
<img src="https://github.com/cthedark/Translumo/blob/master/docs/cthedark-fork-demo.gif">
<img src="https://github.com/cthedark/Translumo/blob/master/docs/cthedark-fork-demo2.gif">
<h1>How to use</h1>
<ol>
  <li>Open the Settings (alt+g by default)</li>
  <li>Select Languages->Source language and Languages->Translation language</li>
  <li>Select Text recognition->Engines (please check Usage tips for recommendation modes)</li>
  <li>Select capture area</li>
  <li>Run translation</li>
</ol> 
<h1>Usage tips</h1>
Generally, I recommend always keep Windows OCR turned on. This is the most effective OCR for the primary text detection with less impact on performance. 
<h3>Recommended combinations of OCR engines</h3>
<ul>
  <li><b>Tesseract-Windows OCR-EasyOCR</b> - advanced mode with the highest precision</li>
  <li><b>Tesseract-Windows OCR</b> - noticeably less impact on system performance. It will be enough for cases when text has simple solid background and font is quite common</li>
  <li><b>Windows OCR-EasyOCR</b> - for very specific complex cases it makes sense to disable Tesseract and avoid unnecessary text noises</li>
</ul>
<h3>Select minimum capture area</h3>
<p>It reduces chances of getting into the area random letters from background. Also the larger frame will take longer to process.</p>
<h3>Use proxy list to avoid blocking by translation services</h1>
<p>Some translators sometimes block client for a large number of requests. You can configure personal/shared IPv4 proxies (1-2 should be enough) on <b>Languages->Proxy tab</b>. The application will alternately use proxies for requests to reduce number from one IP address.</p>
<h3>Use Borderless/Windowed modes in games (not Fullscreen)</h3>
<p>It is necessary to display the translation window overlay correctly.</p>
<p>If the game doesn't have such mode, you can use external tools to make it borderless (e.g. <a href="https://github.com/Codeusa/Borderless-Gaming">Borderless Gaming</a>)</p>
<h3>Install the application on SSD</h3>
<p>To reduce cold launch time with enabled EasyOCR engine (loading large EasyOCR model into RAM).</p>
<h1>FAQ</h3>
<h4>I got error "Failed to capture screen" or nothing happens after translation starts</h4>
<p>Make sure that target window with text is active. Also try to restart Translumo or reopen target window.</p>
<h4>I got error "Text translation is failed" after successful using the translation</h4>
<p>There is a high probability that translation service temporarily blocked requests from your IP. You can change translator or configure proxy list.</p>
<h4>Can't enable Windows OCR</h4>
<p>Make sure that the application is runned as an Administrator. Translumo each time tries check installed Windows language pack via PowerShell.</p>
<h4>I set borderless/windowed mode, but a translation window is still displayed under a game</h4>
<p>When game is running and focused use the hotkey (ALT+T by default) to hide and then show again translation window</p>
<h4>Package downloading for EasyOCR failed</h4>
<p>Try to re-install it under VPN</p>
<h4>Hotkeys don't work</h4>
<p>Other applications may intercept specific hotkeys handling</p>
<h4>I got error "Text detection is failed (TesseractOCREngine)"</h4>
<p>Make sure that the application path contains only latin letters.</p>
<h1>Build</h1>
<p><i>Visual Studio 2022 and .NET 7 SDK are required</i></p>
<ul>
  <li>Clone repository (<b>master</b> branch always equals last release version):</li>
  
```bash
git clone https://github.com/cthedark/Translumo.git
```
  <li>Restore packages and build a solution. <b>binaries_extract.bat</b> will be executed during building, which will automatically download models and python binaries to target output directory.</li>
</ul>
<h1>Credits</h1>
<ul>
  <li><a href="https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit">Material Design In XAML Toolkit</a></li>
  <li><a href="https://github.com/charlesw/tesseract">Tesseract .NET wrapper</a></li>
  <li><a href="https://github.com/shimat/opencvsharp">Opencvsharp</a></li>
  <li><a href="https://github.com/pythonnet/pythonnet">Python.NET</a></li>
  <li><a href="https://github.com/JaidedAI/EasyOCR">EasyOCR</a></li>
  <li><a href="https://github.com/snakers4/silero-models">Silero TTS</a></li>
</ul>
