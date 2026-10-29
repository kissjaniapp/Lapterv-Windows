# Hat eredeti xPrg minta – gépi formátum alapjai

A felhasználó által biztosított `09_01_51_1.xPrg`–`09_01_51_6.xPrg` XML fájlokban a tábla 2800 × 2070 mm (kétféle tájolás), a lapvastagság 18 mm, a fűrészrés 5 mm, a szélezési méret 15 mm. A `SchemaDoc/Header/Material/Pattern/Cut` struktúra egyezik.

A `Cut` bejegyzések száma: 25, 35, 39, 58, 56, 76, összesen 289. A `Y` blokkok száma összesen 22; ezeken belül `X` szegmensek, és olykor `U`/`V` alábontások láthatók. A `P`, `RY`, `RX`, `F` kódok további paraméterezést/jelölést adnak. Az `F=0` nem bizonyíthatóan önálló vágás. A `Rip=1` mindegyik fájlban; ezért két réteg vágásának kódja továbbra sem igazolt.

A modul csak **olvas és validál**. A vezérlés tényleges végrehajtási sorrendje, `U/V/F` pontos szemantikája és a gépbe tölthetőség **nem bizonyított**. A korábbi `.xMix` másik munkához tartozott. Ne adjuk ki az itt olvasott mintákból automatikusan gépi `.xPrg` exportot, mielőtt azt a gép konkrét vezérlésén ellenőriztük.
