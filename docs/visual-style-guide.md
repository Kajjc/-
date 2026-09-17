# «Арена переговоров» — гайд по визуальному стилю и промпты для генерации ассетов

Документ для команды: единое визуальное направление продукта и готовые промпты
для ChatGPT/Grok, чтобы за один проход собрать консистентный набор картинок
(портреты оппонентов, фоны сцен, иконки навыков/исходов). Экономит балл по
п.5 ТЗ («продуманный, аккуратный и приятный интерфейс») и п.8.4 («оформление
усиливает опыт, а не отвлекает от сути»).

Это документ про то, **как ассеты должны выглядеть художественно** — не про
раскладку экрана. Раскладка (`unity/Assets/Scripts/UI/DialogueUIController.cs`)
пока строит UI кодом без слотов под изображения — добавление `Image`-компонентов
под портрет/фон/иконки отдельная задача имплементации, вне охвата этого файла.

---

## 1. Единое визуальное направление

**Стиль одной фразой**: современная корпоративная флэт-иллюстрация с мягкими
градиентами — не фотореализм, не мультяшность.

Почему именно так:
- **Не фотореализм лиц.** Три отдельных портрета, сгенерированных как фото
  реальных людей, почти гарантированно разъедутся по качеству кожи, освещению
  и «зловещей долине» между генерациями — это заметно и портит впечатление
  единого продукта больше, чем любая стилизация.
- **Не мультяшность/casual.** Аудитория — HR/продажи/закупки, корпоративное
  обучение (см. `docs/product-concept.md`), а не массовый игрок. Мультяшный
  или чиби-стиль читается как несерьёзный инструмент для взрослой деловой
  аудитории и противоречит позиционированию.
- **Флэт с мягкими градиентами** — золотая середина: достаточно «дизайнерский»
  и современный (плоские формы, мягкая тень/градиент вместо жёсткого фотосвета),
  но при этом стабильно воспроизводится image-моделями от портрета к портрету,
  от иконки к иконке — потому что не требует точного фотографического сходства.

**Настроение**: деловое, но не скучное — сдержанная строгая композиция
(костюмы, переговорные комнаты), но не монохромная унылая палитра: тёплые
акценты (янтарь, коралл) поверх холодной база (тёмно-синий, стальной тил)
дают ощущение напряжения и энергии переговоров, а не корпоративного буклета.

**Палитра (HEX)** — используется во всех промптах ниже через единый суффикс:

| Роль в палитре | Название | HEX |
|---|---|---|
| Тёмная база / авторитет | Deep Navy | `#16233F` |
| Вторичный тёмный / панели | Slate Blue | `#2E4368` |
| Холодный акцент / логика, спокойствие | Steel Teal | `#4C8FA6` |
| Тёплый акцент / энергия, компромисс | Warm Amber | `#E8A33D` |
| Тёплый акцент / напор, провал | Muted Coral | `#D9694F` |
| Позитивный акцент / выигрыш | Sage Green | `#5FA378` |
| Светлый нейтральный / бумага, фон панелей | Parchment | `#F3EFE7` |
| Линии/текст на светлом | Charcoal Ink | `#23262B` |

Смысловое закрепление цвета за игровыми сущностями (для консистентности между
иконками и остальным UI, которое команда будет красить дальше):
- **Напор** → Muted Coral / Warm Amber (энергия, давление).
- **Эмпатия** → Steel Teal / Parchment (мягкость, спокойствие).
- **Логика** → Deep Navy / Slate Blue (строгость, структура).
- **Win** → Sage Green + Warm Amber.
- **Compromise** → Warm Amber + Steel Teal.
- **Fail** → Muted Coral + Deep Navy.

---

## 2. Единый style suffix (вставлять дословно в конец КАЖДОГО промпта)

Чтобы весь набор выглядел как один продукт, а не случайные картинки, в конец
каждого промпта ниже добавляется **один и тот же** блок текста без изменений:

```
Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Этот блок уже вставлен целиком в конец каждого промпта в §3 ниже — редактировать его
нужно только здесь и здесь же вручную продублировать во все 13 промптов, если вы
меняете палитру/стиль. Копировать промпт для генерации нужно **целиком одним куском**,
из одного code-блока — ничего вручную дописывать/склеивать не требуется.

---

## 3. Промпты по категориям ассетов

### 3.1 Портреты оппонентов (3 шт.)

**Технические требования (общие для всех трёх)**: квадратный кадр 1:1, экспорт
1024×1024 px (лучше 2048×2048 для запаса под WebGL-масштабирование), **прозрачный
фон** (PNG с альфа-каналом), поясной портрет (chest-up) в развороте 3/4 к
камере, персонаж занимает ~75–85% высоты кадра с равномерными полями по краям
(чтобы при масштабировании под угол диалогового экрана не появлялось лишнего
пустого фона с одной стороны).

**Портрет 1 — Руководитель (HR-сценарий)**
Тон по `docs/negotiation-domains.md`: нейтральный → раздражённый при эскалации,
держит бюджет и решение о повышении.

```
Portrait bust of a composed department manager in his late 40s conducting a
performance review, calm but faintly guarded expression, one eyebrow slightly
raised, hands loosely clasped in front of his chest, wearing a tailored dark
blazer over an open-collar shirt, three-quarter angle facing camera, a
neutral-to-tense authority figure who controls the budget, corporate office
context only implied through soft blurred shapes behind him, tight chest-up
crop with even padding on all sides, transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Русское пояснение: портрет для сценария пересмотра зарплаты — база нейтральная,
но с лёгкой настороженностью, чтобы поза читалась и как «спокойный руководитель»,
и как готовый быстро перейти к раздражению при эскалации разговора.

**Портрет 2 — Закупщик клиента (B2B-продажи)**
Тон: напористый/скептический, «у конкурента дешевле», давит на цену.

```
Portrait bust of a sharp, skeptical corporate procurement lead in her mid-30s,
arms crossed, one eyebrow raised in a challenging "convince me" expression, a
slight smirk suggesting she already has a cheaper competing offer in hand,
sleek modern business attire, three-quarter angle facing camera, confident and
mildly combative posture, tight chest-up crop with even padding on all sides,
transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Русское пояснение: оппонент в сценарии защиты цены — закупщик клиента,
скептичный и давящий с самого начала разговора, визуально «противник», а не
партнёр.

**Портрет 3 — Менеджер поставщика (Закупки)**
Тон: плавает от «мы партнёры» до защитного «у нас и так по нижней границе
маржи».

```
Portrait bust of a warm but guarded supplier account manager in his early 40s,
a closed-lip half-smile that reads as friendly yet cautious, holding a folder
subtly against his chest like a protective barrier, business-casual attire
with sleeves rolled up, three-quarter angle facing camera, partnership-oriented
but defensive body language, tight chest-up crop with even padding on all
sides, transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Русское пояснение: оппонент в закупочном сценарии — единственный портрет из
трёх, который должен выглядеть двойственно (не враг и не начальник, а партнёр,
который защищает свои цифры), отсюда папка у груди как визуальный барьер.

---

### 3.2 Фоновые сцены (3 шт.)

**Технические требования (общие для всех трёх)**: широкоформатный кадр 16:9,
экспорт минимум 1920×1080 px, **непрозрачный** фон (это самый нижний слой
Canvas — альфа не нужна), пустая комната без людей (портрет оппонента ляжет
поверх отдельным слоем), неглубокая глубина резкости/мягкое боуке на дальнем
плане, чтобы фон не спорил с текстом и элементами интерфейса поверх него.

**Фон 1 — Кабинет руководителя (HR)**

```
Empty modern manager's office interior set up for a performance-review
meeting, mid-century-influenced corporate furniture, a clean desk with a
closed laptop and a single neat stack of papers, a large window with a softly
blurred city skyline, warm late-afternoon light mixing with cool interior
tones, shallow depth of field with soft bokeh, quiet and slightly tense
atmosphere, empty room with no people, wide 16:9 establishing shot,
Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Фон 2 — Переговорная клиента (B2B-продажи)**

```
Empty modern client boardroom set up for a high-stakes sales negotiation, a
glass-walled meeting room, a long conference table with a blurred presentation
screen showing indistinct chart shapes in the background, a city high-rise
view through floor-to-ceiling windows, cool corporate lighting with a
competitive, high-pressure atmosphere, shallow depth of field with soft
bokeh, empty room with no people, wide 16:9 establishing shot, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Фон 3 — Офис поставщика (Закупки)**

```
Empty supplier company meeting space that blends a small office corner with a
glimpse of a logistics warehouse through an interior glass partition, softly
blurred stacked pallets and shipment boxes visible in the background, a
simple meeting table in the foreground, warm practical lighting suggesting a
working business rather than a showroom, shallow depth of field with soft
bokeh, empty room with no people, wide 16:9 establishing shot, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Русское пояснение (все три): фон должен молча считываться как площадка
конкретного сценария (кабинет / переговорка клиента / склад-офис поставщика)
без единого слова текста на изображении — контекст сцены игрок узнаёт из
реплик, картинка только настроение.

---

### 3.3 Иконки навыков и исходов (6 шт.)

**Технические требования (общие для всех шести)**: квадратный кадр 1:1,
экспорт 512×512 px, **прозрачный фон** (под `Image`-компоненты, которые можно
тонировать/масштабировать), один сплошной силуэт с мягкой градиентной
заливкой (не тонкий line-art), объект по центру с равными отступами от края
кадра — это важно, чтобы все шесть иконок при уменьшении до ~48–64px в игре
визуально «весили» одинаково.

**Напор**

```
Flat icon of a single bold forward-pointing chevron arrow with dynamic speed
lines trailing behind it, symbolizing assertive forward momentum, rendered as
one solid shape with a soft gradient fill from muted coral #D9694F to warm
amber #E8A33D, centered composition with even padding on all sides,
transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Эмпатия**

```
Flat icon of two overlapping speech-bubble shapes with a small soft heart
nested in the area where they overlap, symbolizing empathetic listening,
rendered as one solid shape with a soft gradient fill from steel teal #4C8FA6
to parchment off-white #F3EFE7, centered composition with even padding on all
sides, transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Логика**

```
Flat icon of a simplified balance scale merged with a small gear at its
pivot point, symbolizing analytical reasoning, rendered as one solid shape
with a soft gradient fill from deep navy #16233F to slate blue #2E4368,
centered composition with even padding on all sides, transparent background,
Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Исход: Win**

```
Flat icon of a minimal laurel wreath wrapping around a single checkmark,
symbolizing a winning negotiation outcome, rendered as one solid shape with a
soft gradient fill from sage green #5FA378 to warm amber #E8A33D, centered
composition with even padding on all sides, transparent background,
Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Исход: Compromise**

```
Flat icon of a handshake formed from two halves rendered in two different
accent tones meeting in the middle, symbolizing a compromise outcome,
rendered as one solid shape with a soft gradient fill from warm amber #E8A33D
to steel teal #4C8FA6, centered composition with even padding on all sides,
transparent background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

**Исход: Fail**

```
Flat icon of a downward-pointing broken chevron arrow split by a small
jagged crack, symbolizing a failed negotiation outcome, rendered as one
solid shape with a soft gradient fill from muted coral #D9694F to deep navy
#16233F, centered composition with even padding on all sides, transparent
background, Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

---

### 3.4 Опционально — титульный/лого-арт стартового экрана

**Технические требования**: широкоформатный кадр 16:9, экспорт минимум
1920×1080 px, непрозрачный фон (фоновая картинка стартового экрана), верхняя
часть кадра художественно «пустая» (мягкий градиентный воздух), чтобы там
естественно читалось название продукта, которое накладывается поверх отдельно
— это не решение по раскладке, а свойство самой композиции картинки.

```
Wide symbolic hero illustration of two silhouetted business figures seated
across a minimalist negotiation table, facing each other in a tense but
composed stand-off, a dramatic single overhead spotlight creating a shared
pool of light between them while the rest of the room fades into soft
gradient darkness, generous empty gradient space in the upper third of the
frame, no faces in detail, no text, wide 16:9 cinematic composition,
Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.
```

Русское пояснение: не обязательный ассет, но недорогой по эффекту — один общий
хиро-арт для главного экрана/загрузки, закрепляющий стиль ещё до первого
диалога.

---

## 4. Итого промптов

| Категория | Количество |
|---|---|
| Портреты оппонентов | 3 |
| Фоновые сцены | 3 |
| Иконки навыков | 3 |
| Иконки исходов | 3 |
| Титульный арт (опционально) | 1 |
| **Всего** | **13** |

---

## 5. ChatGPT vs Grok — какой инструмент использовать

Я поискал свежие (2026) сравнения ChatGPT/GPT-Image и Grok Imagine именно по
консистентности стилизованных портретов и получил **противоречивые результаты
из источников без раскрытой методологии** — не бенчмарки, а SEO-обзорные
блоги:

- Часть источников утверждает, что ChatGPT даёт более «чистые» и предсказуемые
  результаты, подходящие для деловых/маркетинговых материалов —
  [Comparison: Image generation models in 2026 (Medium)](https://mickey19951031.medium.com/comparison-image-generation-models-in-2026-gemini-vs-grok-vs-chatgpt-vs-stable-5098ea38e7ae),
  [ChatGPT Images 2.0 vs Grok Imagine (Techloy)](https://www.techloy.com/chatgpt-images-2-0-vs-grok-imagine-which-ai-image-generator-should-you-use/).
- Другая часть источников утверждает прямо противоположное — что именно
  **Grok Imagine 2 лучше держит консистентность персонажа** между отдельно
  сгенерированными изображениями, а GPT-Image, наоборот, не гарантирует
  идентичность персонажа в серии генераций —
  [GPT Image 2 vs Grok Imagine, 6 category test (AtlasCloud)](https://www.atlascloud.ai/blog/guides/gpt-image-2-vs-grok-imagine-image),
  [Grok Imagine vs GPT-Image-2 benchmark (Vidguru)](https://www.vidguru.ai/blog/grok-imagine-image-quality-vs-gpt-image-2-comparison.html),
  [GPT Image 2 vs Grok Imagine, 6 real image tests (SuperMaker AI)](https://supermaker.ai/blog/gpt-image-2-vs-grok-imagine-image-20-6-real-image-tests/),
  [GPT-Image-2 vs Grok Imagine Image 2.0 (OrcaRouter)](https://www.orcarouter.ai/blog/gpt-image-2-vs-grok-imagine-image-2-0).

**Вывод: однозначных надёжных данных нет.** Источники взаимно противоречат
друг другу, ни один не публикует прозрачную методологию тестирования, и почти
все читаются как шаблонные SEO-статьи, а не независимый бенчмарк — доверять им
как основанием для выбора инструмента нельзя.

**Рекомендация — пилот перед основным прогоном:**
1. Сгенерируйте **один и тот же** промпт портрета (например, руководитель из
   п.3.1) и **один и тот же** промпт фона в ChatGPT и отдельно в Grok.
2. Сравните результат по двум критериям: (а) насколько точно соблюдён
   `Modern corporate flat-illustration style with soft painterly gradients, clean
semi-geometric shapes and crisp vector-like edges, no photorealistic skin or
photo texture, no cartoon/chibi/anime proportions, restrained professional
business mood with a touch of warmth, single soft studio light source from
upper-left with a gentle rim light and subtle ambient occlusion, muted matte
color palette limited to deep navy #16233F, slate blue #2E4368, steel teal
#4C8FA6, warm amber #E8A33D, muted coral #D9694F, sage green #5FA378 and
parchment off-white #F3EFE7, minimal fine detail, no text, no watermark, no
logo, no signature.` — палитра, отсутствие фотореализма, отсутствие
   мультяшности; (б) субъективно, насколько стабильным кажется стиль между
   портретом и фоном внутри одного инструмента.
3. **Выберите один инструмент и сгенерируйте им весь набор из 13 промптов.**

**Явное предупреждение: не смешивайте инструменты внутри одного набора
ассетов.** Даже с одинаковым текстовым промптом и одинаковым style suffix
разные модели по-разному интерпретируют «flat corporate illustration» —
разная трактовка градиентов, разные пропорции лиц, разная плотность деталей.
Три портрета и три фона, сгенерированные в разных инструментах, будут заметно
не похожи друг на друга даже при идентичном тексте — это разрушит именно то
единство визуального языка, ради которого вводится style suffix, и будет
считываться жюри как «набор случайных картинок», а не продуманный интерфейс
(обратный эффект от заявленного в п.5 ТЗ).

Дополнительно: генерируйте все ассеты одного инструмента по возможности в
одной сессии/диалоге (а не в разрозненных новых чатах) — некоторые инструменты
подхватывают контекст предыдущих генераций в рамках сессии, что дополнительно
повышает похожесть между картинками одного набора.

Sources:
- [Comparison: Image generation models in 2026 — Gemini vs Grok vs ChatGPT vs Stable (Medium)](https://mickey19951031.medium.com/comparison-image-generation-models-in-2026-gemini-vs-grok-vs-chatgpt-vs-stable-5098ea38e7ae)
- [ChatGPT Images 2.0 vs Grok Imagine: Which AI Image Generator Should You Use? (Techloy)](https://www.techloy.com/chatgpt-images-2-0-vs-grok-imagine-which-ai-image-generator-should-you-use/)
- [We Tested GPT Image 2 vs Grok Imagine with 6 Category Tests (AtlasCloud)](https://www.atlascloud.ai/blog/guides/gpt-image-2-vs-grok-imagine-image)
- [Grok Imagine Image Quality vs OpenAI GPT-Image 2: The 2026 AI Image Benchmark (Vidguru)](https://www.vidguru.ai/blog/grok-imagine-image-quality-vs-gpt-image-2-comparison.html)
- [GPT Image 2 vs Grok Imagine Image 2.0: 6 Image Tests (SuperMaker AI)](https://supermaker.ai/blog/gpt-image-2-vs-grok-imagine-image-20-6-real-image-tests/)
- [GPT-Image-2 vs Grok Imagine Image 2.0: The #2 Challenger (OrcaRouter)](https://www.orcarouter.ai/blog/gpt-image-2-vs-grok-imagine-image-2-0)
