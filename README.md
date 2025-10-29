[](https://opensource.org/licenses/Apache-2.0)
[]()
[](https://trello.com/b/MEHnLySw/translumo)

Advanced screen translator. **Translumo** is able to detect and translate appearing in the selected area text in real-time (e.g. subtitles).

Fork from [Danily07](https://github.com/Danily07/Translumo) - the main purpose is to make this work locally and better optimized for playing games/visual novels

# Main features

  * **High text recognition precision**
    Translumo allows to combine the usage of several OCR engines simultaneously. It uses machine learning training model for scoring each recognized result by OCR and chooses the best one.

  * **Simple interface**
    The main idea was to make tool, that does not require manual adjustments for each case and convenient for everyday use.

  * **Low latency**
    There are several implemented optimizations to reduce impact on system performance and minimize latency between the moment a text appears and actual translation.

  * **Integrated modern OCR engines:** Tesseract 5.2, WindowsOCR, EasyOCR

  * **Available translators:** Google Translate, Yandex Translate, DeepL, Locally run [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) (Papago is removed as it's now a paid service from ncloud)

  * **Available recognition languages:** English, Russian, Japanese, Chinese (simplified), Korean

  * **Available translation languages:** English, Russian, Japanese, Chinese (simplified), Korean, French, Spanish, German, Portuguese, Italian, Vietnamese, Thai, Turkish, Arabic

# Differences from [the original](https://github.com/Danily07/Translumo)

  * Added a way to manually translate by a button press while using the selected translation area
  * Removed the defunct Papago option
  * Added a way to interface with a locally running server of your choosing. I have tested with:
    *  [LM Studio](https://lmstudio.ai/) (you have to turn on the server)
    *  [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate).
    *  My personal flask based translation server. ([here](https://github.com/cthedark/flask-decoder-encoder-model-translate))
  * For the locally running server, there are new fields that you have to populate in the Language section of the setting UI.
    * API Endpoint like `http://localhost:1234/v1/chat/completions`
    * How to bundle the text as a payload. It's a JSON blob with `[content]` placeholder to be replaced with the source text.
  ```json
  {
    "model": "gemma-2-2b-jpn-it-translate",
    "messages": [
      { "role": "system", "content": "Translate to English without any explanation." },
      { "role": "user", "content": "[content]" }
    ],
    "temperature": 0.7,
    "max_tokens": -1,
    "stream": false
  }
  ```
    * How to find the value. Use the standard JSON object path format like `choices[0].message.content`.
  * Added an option to record all translations and reuse them without hitting the service again. This means one can also edit the translated strings to be used later by a different user, or we can also turn these into training data.

# Planned Features

  * Add back Papago option using their new paid cloud service (API key is needed).
  * More UX optimization around running games.
  * Korean Translation

# System requirements

  * Windows 10 build 19041 (20H1) / Windows 11
  * DirectX11
  * 8 GB RAM *(for mode with EasyOCR)*
  * 5 GB free storage space *(for mode with EasyOCR)*
  * Nvidia GPU with CUDA SDK 11.8 support (GTX 750, 8xxM, 9xx series or later) *(for mode with EasyOCR)*

# Demonstration

# How to use

1.  Open the Settings (alt+g by default)
2.  Select Languages-\>Source language and Languages-\>Translation language
3.  Select Text recognition-\>Engines (please check Usage tips for recommendation modes)
4.  Select capture area
5.  Run translation

# Usage tips

Generally, I recommend always keep Windows OCR turned on. This is the most effective OCR for the primary text detection with less impact on performance.

### Recommended combinations of OCR engines

  * **Tesseract-Windows OCR-EasyOCR** - advanced mode with the highest precision
  * **Tesseract-Windows OCR** - noticeably less impact on system performance. It will be enough for cases when text has simple solid background and font is quite common
  * **Windows OCR-EasyOCR** - for very specific complex cases it makes sense to disable Tesseract and avoid unnecessary text noises

### Select minimum capture area

It reduces chances of getting into the area random letters from background. Also the larger frame will take longer to process.

### Use proxy list to avoid blocking by translation services

Some translators sometimes block client for a large number of requests. You can configure personal/shared IPv4 proxies (1-2 should be enough) on **Languages-\>Proxy tab**. The application will alternately use proxies for requests to reduce number from one IP address.

### Use Borderless/Windowed modes in games (not Fullscreen)

It is necessary to display the translation window overlay correctly.

If the game doesn't have such mode, you can use external tools to make it borderless (e.g. [Borderless Gaming](https://github.com/Codeusa/Borderless-Gaming))

### Install the application on SSD

To reduce cold launch time with enabled EasyOCR engine (loading large EasyOCR model into RAM).

# FAQ

#### I got error "Failed to capture screen" or nothing happens after translation starts

Make sure that target window with text is active. Also try to restart Translumo or reopen target window.

#### I got error "Text translation is failed" after successful using the translation

There is a high probability that translation service temporarily blocked requests from your IP. You can change translator or configure proxy list.

#### Can't enable Windows OCR

Make sure that the application is runned as an Administrator. Translumo each time tries check installed Windows language pack via PowerShell.

#### I set borderless/windowed mode, but a translation window is still displayed under a game

When game is running and focused use the hotkey (ALT+T by default) to hide and then show again translation window

#### Package downloading for EasyOCR failed

Try to re-install it under VPN

#### Hotkeys don't work

Other applications may intercept specific hotkeys handling

#### I got error "Text detection is failed (TesseractOCREngine)"

Make sure that the application path contains only latin letters.

# Build

*Visual Studio 2022 and .NET 7 SDK are required*

  * Clone repository (**master** branch always equals last release version):

    ```bash
    git clone https://github.com/cthedark/Translumo.git
    ```

  * Restore packages and build a solution. **binaries\_extract.bat** will be executed during building, which will automatically download models and python binaries to target output directory.

# Credits

  * [Material Design In XAML Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
  * [Tesseract .NET wrapper](https://github.com/charlesw/tesseract)
  * [Opencvsharp](https://github.com/shimat/opencvsharp)
  * [Python.NET](https://github.com/pythonnet/pythonnet)
  * [EasyOCR](https://github.com/JaidedAI/EasyOCR)
  * [Silero TTS](https://github.com/snakers4/silero-models)