# Промты для генерации портретов (ChatGPT / DALL·E)

Готовые промты для портретов говорящих персонажей. Заменяют стилизованные плейсхолдеры
`RiotGalaxy.Content/Images/portrait_*.png`. После генерации: сохранить квадратные PNG,
по имени из таблицы ниже, положить в `Images/`, фон убрать в прозрачность (как враги —
см. `tools`/скрипт нарезки) и пересобрать контент.

> Важно про ChatGPT: он **откажется** рисовать реальных людей и персонажей из чужих
> вселенных (Вейдер, Зепп Бранниган, Путин и т.п.). Поэтому промты описывают **архетип/образ**,
> а не конкретного героя. Имён-отсылок в промтах нет — узнаваемость даём через детали.

---

## Общий стиль (вставляется в каждый промт — уже включён ниже)

> **Style:** flat-shaded cartoon vector portrait, bold clean outlines, vibrant saturated colors,
> soft cel shading, head-and-shoulders bust, centered, facing the viewer, friendly humorous
> retro-arcade sci-fi tone (Space Invaders era, indie mobile shooter). Square 1:1, simple flat
> solid-color background, even soft lighting, no text, no logos, no watermark.

## Как держать единый набор (советы)

- Генерируй **все портреты в одном чате подряд** — модель сохраняет стиль предыдущих.
- Проси формат **1024×1024, 1:1**, лицо в верхней-центральной части (оставить «воздух» сверху).
- Проси **plain solid background** (один цвет) — так проще вырезать в прозрачность.
- Добавляй в конце: `same art style, line weight and lighting as the previous portrait` (со 2-го).
- Не нужен текст/подписи на картинке: `no text, no letters`.

## Карта файлов

| Файл в игре | Персонаж | Акцентный цвет фона |
|---|---|---|
| `portrait_luke` | Люк СкайРокер (игрок) | глубокий синий |
| `portrait_raider` | Дарк Рейдер (командир) | угольно-чёрный |
| `portrait_kardigan` | Чехх Кардиган (командующий) | янтарно-оранжевый |
| `portrait_agdam` | Агдам ДоДыров (мажор) | фиолетовый |
| `portrait_gaechka` | Бригадир Гаечка (шахтёр) | жёлтый |
| `portrait_ognev` | Брандмайор Огнев (пожарный) | красный |
| `portrait_serdyuk` | Главврач Сердюк (медик) | бирюзовый |
| `portrait_podorozhnik` | ИИ-Подорожник (робот) | зелёный |
| `portrait_zahar` | Ст. смены Захар (охрана) | оливковый |
| `portrait_zonalny` | Наместник Зональный (злодей) | пурпур + золото |

---

## Промты по персонажам (вставлять как есть)

### portrait_luke — Люк СкайРокер
> Flat-shaded cartoon vector portrait of a weary but charming human starfighter ace pilot,
> late 30s, light stubble, tired confident half-smile, wearing a white-and-blue flight helmet
> with a tinted visor pushed up, blue flight-suit collar. Bold clean outlines, soft cel shading,
> head-and-shoulders bust, centered, facing viewer. Square 1:1, flat deep-blue solid background,
> even lighting, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_raider — Дарк Рейдер
> Flat-shaded cartoon vector portrait of an imposing space-fleet commander wearing a full matte-black
> angular battle helmet that completely hides the face, glowing red triangular eye-slits, a ribbed
> breathing grille over the mouth, dark high cape collar. Menacing but stoic. Bold clean outlines,
> soft cel shading, bust centered, facing viewer. Square 1:1, flat charcoal-black solid background,
> dramatic rim light, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_kardigan — Чехх Кардиган
> Flat-shaded cartoon vector portrait of a pompous square-jawed space-navy commander with an
> exaggerated huge chin, slicked dark hair, smug overconfident grin, ornate blue military uniform
> with gold trim and a tall collar. Comedic buffoon energy. Bold clean outlines, soft cel shading,
> bust centered, facing viewer. Square 1:1, flat amber-orange solid background, no text, no logos.
 plain solid background. resolution 1024x1024. same art style as previous
 
### portrait_agdam — Агдам ДоДыров
> Flat-shaded cartoon vector portrait of a spoiled rich young rookie pilot with a smug entitled
> smirk, slicked-back brown hair, designer dark sunglasses, a flashy popped collar and a tiny golden
> medal. Punchable fat mama's-boy vibe. Bold clean outlines, soft cel shading, bust centered, facing
> viewer. Square 1:1, flat purple solid background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_gaechka — Бригадир Гаечка
> Flat-shaded cartoon vector portrait of a gruff asteroid-mining foreman, heavy stubble, tired
> squint, smudges of dirt, a bright yellow hard hat with a headlamp. Working-class space miner.
> Bold clean outlines, soft cel shading, bust centered, facing viewer. Square 1:1, flat industrial-
> yellow solid background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_ognev — Брандмайор Огнев
> Flat-shaded cartoon vector portrait of a bombastic space fire-brigade chief with a big bushy
> mustache, a red firefighter helmet with a ridged crest, a determined frown. Bold clean outlines,
> soft cel shading, bust centered, facing viewer. Square 1:1, flat fire-red solid background,
> no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_serdyuk — Главврач Сердюк
> Flat-shaded cartoon vector portrait of an exhausted overworked chief space-doctor, grey hair,
> a round reflective head-mirror on the forehead, white medical coat collar, weary polite smile.
> Bold clean outlines, soft cel shading, bust centered, facing viewer. Square 1:1, flat teal solid
> background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_podorozhnik — ИИ-Подорожник
> Flat-shaded cartoon vector portrait of a goofy medical assistant robot: a rounded screen-face
> displaying simple glowing green pixel eyes and a flat mouth line, a small antenna, dark metal
> casing. Cheerful but dim. Bold clean outlines, soft cel shading, centered, facing viewer.
> Square 1:1, flat green solid background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_zahar — Ст. смены Захар
> Flat-shaded cartoon vector portrait of a stern budget shopping-mall security guard in space,
> a peaked cap with a cheap golden badge, flat humorless mouth, slightly bored expression, plain
> uniform collar. Bold clean outlines, soft cel shading, bust centered, facing viewer. Square 1:1,
> flat olive-green solid background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

### portrait_zonalny — Наместник Зональный
> Flat-shaded cartoon vector portrait of a smug corrupt colonial governor, slicked blond hair,
> a thin pencil mustache, a golden monocle, an opulent gold-trimmed collar, an oily self-satisfied
> smile. Embezzler villain vibe. Bold clean outlines, soft cel shading, bust centered, facing
> viewer. Square 1:1, flat royal-purple solid background with subtle gold accent, no text, no logos.
 plain solid background. resolution 1024x1024. same art style as previous
 
---

## Бонус — персонажи Актов II–III (понадобятся позже)

### portrait_emperor — Император
Образ — **архетип медлительного всесильного самодержца** (узнаётся через роль/регалии/холод, а не
через лицо конкретного человека). Персонаж вымышленный.
> Flat-shaded cartoon vector portrait of an aging absolute autocrat space-emperor: cold pale
> grey-blue eyes, flat unreadable expressionless stare, thin tight lips, balding short grey hair,
> a severe high-collar imperial uniform with heavy gold insignia and rows of medals, a fictional
> double-headed-eagle emblem on the chest. Aura of slow, untouchable, absolute power. Bold clean
> outlines, soft cel shading, bust centered, facing viewer. Square 1:1, flat dark-red solid
> background, no text, no logos. plain solid background. resolution 1024x1024. same art style as previous

Опционально — «тронная» сцена для заставки (широкий кадр, НЕ для портретной рамки диалога):
> Wide cinematic flat-shaded cartoon illustration: an aging absolute autocrat space-emperor sitting
> alone on an imposing golden throne at the far end of an absurdly long ceremonial table, in a cold
> cavernous marble hall, tiny distant officials standing far away, dramatic god-rays. Conveys
> isolation and total power. Bold clean outlines, soft cel shading, no text, no logos.

### portrait_trapp — DJ Trapp
> Flat-shaded cartoon vector portrait of an energetic dim-witted alien-faction leader styled as a
> flashy space DJ: bright red hair, oversized neon headphones, gaudy jacket, a goofy overconfident
> grin, hands frozen mid-gesture. Bold clean outlines, soft cel shading, bust centered, facing
> viewer. Square 1:1, flat hot-pink/cyan solid background, no text, no logos.
