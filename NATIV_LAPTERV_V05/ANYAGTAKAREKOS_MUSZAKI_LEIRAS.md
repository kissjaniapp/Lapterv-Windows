# Új vágási stratégia – natív Lapterv 0.5

## Alapelv

A gép nem képes tábla közepén megálló részvágásra. A vágások a pillanatnyilag befogott tábla vagy **már leválasztott csík teljes keresztmetszetén** keresztül futnak:

1. Minden nyers táblából teljesen átvágott, hosszanti **Y sávokat** készítünk.
2. Csak az összes tábla sávozása után adjuk vissza az elkülönített csíkokat a gépnek.
3. A csíkokat **X irányban, teljes szélességben** daraboljuk.
4. A fűrészrést és a szélvágást minden kiosztási kapacitásban figyelembe vesszük.

Ez kétlépcsős guillotine-séma; nem tetszőleges 2D nesting. A szálirány és élzárás miatt alkatrész-elforgatás jelenleg nincs.

## Megtartott hagyományos kiosztás

Az eredeti v0.4-es első-találat sávozás megmaradt. A használó a 02 Szabásterv fül **Vágási mód** legördülőjében vált a hagyományos és az új módszer között. Mindkét terv ugyanazokat az alkatrészeket tartalmazza. A projektfájlban a választás mentésre kerül.

## Gazdaságos mód, 12 jelölt

Először az azonos szélességű darabokat csoportosítjuk; minden szélességhez többféle hossz szerinti sorrend és best-/worst-fit csíkfeltöltés készül. A különböző szélességű csíkokat háromféle sorrendben, szabad Y-hely alapján helyezzük egész táblákba. A hagyományos kiosztás maga is részt vesz az összehasonlításban.

A sorrend kiválasztási szempontjai:

1. A szükséges egész táblák száma a lehető legkisebb legyen a vizsgált jelöltek között.
2. Egyező táblaszám mellett maradjon a legnagyobb egybefüggő, legalább 100 × 100 mm-es visszanyerhető téglalap.
3. További döntetlen esetén a visszanyerhető maradékok teljes területe, majd a sávok száma számít.

A heurisztika **nem garantál globálisan optimális** matematikai eredményt. Bizonyos alkatrészkészleteknél a két terv azonos lehet. A kirajzolt tábla és a kihasználtsági százalék alapján a kezelő választ.

## Megmaradó anyag és ellenőrzés

A végigvágott sávok után egy teljes hosszúságú táblamaradék és sávvégi kisebb téglalapok maradhatnak. Ezek adatai a **Maradéklista (CSV)** gombbal Excelben megnyitható fájlba exportálhatók, így a nagy maradékok félretehetők és később újra megadhatók alaptáblaként. A maradék felhasználhatóságát a kezelőnek kell ellenőriznie (sérülés, méret, élminőség, anyagirány).

Minden létrehozott tervet a `Planner.ValidateGeometry` ellenőriz: megvannak-e a darabok, nincs-e átfedés/kilógás, a sávok teljesen átvághatók-e, számoltak-e a vágásréssel és a szélezéssel. A tesztcsomag véletlenszerű kevert méretű munkákat is ellenőriz.

## Gépi export

A kísérleti `.xPrg`/`.xMix` export most az **éppen kiválasztott** tervet használja. A gép valódi `XPrg`-értelmezése és fizikai végrehajtási sorrendje továbbra **nem igazolt**; a fájlt először gyártói offline szimulátorban kell ellenőrizni. A gépi próba a vezérlés hitelesítése előtt nem biztonságos.
