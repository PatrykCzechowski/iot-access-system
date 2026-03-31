// ============================================================
// Obudowa IoT Access Control — ESP8266 (Wemos D1 Mini) + PN532 NFC/RFID
// Parametryczny model OpenSCAD
// ============================================================
// Użycie z terminala:
//   Podgląd:  openscad enclosure.scad
//   Eksport:  openscad -o enclosure_bottom.stl -D "part=\"bottom\"" enclosure.scad
//             openscad -o enclosure_top.stl    -D "part=\"top\""    enclosure.scad
//             openscad -o enclosure_all.stl    -D "part=\"assembled\"" enclosure.scad
// ============================================================

/* [Wybór części do renderowania] */
part = "assembled"; // ["bottom", "top", "assembled", "exploded"]

/* [Tolerancje druku 3D] */
tol        = 0.3;   // tolerancja wymiaru (mm)
wall       = 2.0;   // grubość ścianki ogólna (mm)
nfc_wall   = 1.5;   // grubość ścianki nad anteną NFC (mm) — max 2mm!
corner_r   = 3.0;   // promień zaokrąglenia narożników

/* [Wemos D1 Mini — ESP8266] */
wemos_l    = 34.5;  // długość PCB
wemos_w    = 25.4;  // szerokość PCB
wemos_pcb  = 1.6;   // grubość PCB
wemos_h    = 6.0;   // profil Z (z komponentami, bez goldpinów)
wemos_usb_w = 11.0; // szerokość otworu USB
wemos_usb_h = 7.0;  // wysokość otworu USB
wemos_reset_d = 2.0;// średnica otworu na reset

/* [PN532 NFC/RFID V3] */
pn532_l    = 42.7;  // długość PCB
pn532_w    = 40.4;  // szerokość PCB
pn532_pcb  = 1.6;   // grubość PCB
pn532_h    = 4.0;   // profil Z komponentów (bez pinów)
pn532_hole_d = 2.2; // średnica otworów montażowych (M2 + luz)
pn532_hole_offset = 2.5; // odległość otworu od krawędzi

/* [Śruby montażowe PN532] */
screw_d      = 2.0;   // M2 gwint
standoff_d   = 4.5;   // średnica słupka
standoff_h   = 3.0;   // wysokość słupka (dystans PCB od dna)

/* [Parametry wewnętrzne] */
cable_channel_w = 8;  // szerokość kanału na przewody
cable_channel_h = 5;  // wysokość kanału na przewody
vent_slot_w     = 1.5;// szerokość szczeliny wentylacyjnej
vent_slot_l     = 12; // długość szczeliny wentylacyjnej
vent_count      = 4;  // ilość szczelin

// ============================================================
// OBLICZENIA AUTOMATYCZNE
// ============================================================

// Wewnętrzne wymiary — PN532 dominuje szerokość
inner_w = max(pn532_w, wemos_w) + 2 * tol + cable_channel_w;
inner_l = pn532_l + 2 * tol + 4; // zapas na kable i montaż
inner_h_bottom = standoff_h + pn532_pcb + pn532_h + 2; // sekcja dolna (PN532 + Wemos)
inner_h_top    = standoff_h + wemos_pcb + wemos_h + 2;  // sekcja górna

// Łączna wysokość wnętrza
inner_h = inner_h_bottom + inner_h_top + cable_channel_h;

// Zewnętrzne wymiary
outer_w = inner_w + 2 * wall;
outer_l = inner_l + 2 * wall;
outer_h_bottom = inner_h * 0.6 + wall; // 60% dół
outer_h_top    = inner_h * 0.4 + nfc_wall; // 40% góra (z cienką ścianką NFC)
outer_h = outer_h_bottom + outer_h_top;

// Lip (zamek zatrzaskowy góra-dół)
lip_h     = 2.0;
lip_thick = 1.2;

// Pozycje modułów wewnątrz
wemos_x = wall + tol + 2;
wemos_y = wall + tol + (inner_w - wemos_w) / 2;
wemos_z = wall + standoff_h;

pn532_x = wall + tol + (inner_l - pn532_l) / 2;
pn532_y = wall + tol + (inner_w - pn532_w) / 2;
pn532_z_in_top = outer_h_top - nfc_wall - pn532_pcb - tol; // blisko górnej ścianki

// ============================================================
// MODUŁY POMOCNICZE
// ============================================================

module rounded_box(size, r) {
    // Prostopadłościan z zaokrąglonymi narożnikami w XY
    hull() {
        for (x = [r, size[0] - r])
            for (y = [r, size[1] - r])
                translate([x, y, 0])
                    cylinder(r = r, h = size[2], $fn = 32);
    }
}

module rounded_box_hollow(outer, wall_t, r) {
    difference() {
        rounded_box(outer, r);
        translate([wall_t, wall_t, wall_t])
            rounded_box([outer[0] - 2*wall_t, 
                         outer[1] - 2*wall_t, 
                         outer[2]], r - wall_t/2);
    }
}

// Słupek montażowy z otworem na śrubę
module standoff(h, outer_d, inner_d) {
    difference() {
        cylinder(d = outer_d, h = h, $fn = 24);
        translate([0, 0, -0.1])
            cylinder(d = inner_d, h = h + 0.2, $fn = 24);
    }
}

// Prowadnica szynowa do trzymania PCB bez otworów montażowych
module pcb_rail(length, pcb_thick, depth) {
    // Szyna w kształcie litery L
    cube([length, depth, pcb_thick + tol]);
    cube([length, 1.2, pcb_thick + tol + 2]);
}

// Szczeliny wentylacyjne
module vent_slots(count, slot_l, slot_w, wall_t) {
    spacing = (slot_l + 3);
    for (i = [0 : count - 1]) {
        translate([i * spacing, 0, 0])
            cube([slot_l, wall_t + 0.2, slot_w]);
    }
}

// Symbol NFC na obudowie (wytłoczenie)
module nfc_symbol(size) {
    // Uproszczony symbol "fal" NFC
    line_w = 0.8;
    for (i = [0 : 2]) {
        r = size * 0.3 + i * size * 0.15;
        difference() {
            cylinder(r = r + line_w/2, h = 0.6, $fn = 48);
            translate([0, 0, -0.1])
                cylinder(r = r - line_w/2, h = 0.8, $fn = 48);
            // Wytnij 3/4 okręgu — zostaw ćwiartkę
            translate([-r*2, -r*2, -0.1])
                cube([r*2, r*4, 1]);
            translate([-r*2, -r*2, -0.1])
                cube([r*4, r*2, 1]);
        }
    }
    // Kropka w centrum
    cylinder(d = size * 0.15, h = 0.6, $fn = 24);
}

// ============================================================
// DOLNA POŁOWA OBUDOWY (BOTTOM)
// ============================================================

module bottom_case() {
    difference() {
        union() {
            // Główna skorupa
            rounded_box_hollow([outer_l, outer_w, outer_h_bottom], wall, corner_r);
            
            // Lip (krawędź do zamknięcia) — wewnętrzna krawędź
            translate([wall - lip_thick, wall - lip_thick, outer_h_bottom - 0.01])
                difference() {
                    rounded_box([outer_l - 2*(wall - lip_thick), 
                                 outer_w - 2*(wall - lip_thick), 
                                 lip_h], corner_r);
                    translate([lip_thick, lip_thick, -0.1])
                        rounded_box([outer_l - 2*wall, 
                                     outer_w - 2*wall, 
                                     lip_h + 0.2], corner_r - lip_thick);
                }
        }
        
        // ---- OTWÓR NA USB (Wemos D1 Mini) — boczna ściana ----
        translate([-0.1, 
                   wemos_y + (wemos_w - wemos_usb_w) / 2,
                   wemos_z + wemos_pcb])
            cube([wall + 0.2, wemos_usb_w, wemos_usb_h]);
        
        // ---- OTWÓR NA PRZYCISK RESET ----
        translate([-0.1,
                   wemos_y + wemos_w * 0.3,  // obok USB
                   wemos_z + wemos_pcb + 2])
            rotate([0, 90, 0])
                cylinder(d = wemos_reset_d, h = wall + 0.2, $fn = 16);
        
        // ---- SZCZELINY WENTYLACYJNE — dno ----
        total_vent_l = vent_count * (vent_slot_l + 3);
        translate([(outer_l - total_vent_l) / 2,
                   (outer_w - vent_slot_w) / 2,
                   -0.1])
            vent_slots(vent_count, vent_slot_l, vent_slot_w, wall);
        
        // ---- SZCZELINY WENTYLACYJNE — boki ----
        for (side = [0, 1]) {
            translate([(outer_l - total_vent_l) / 2,
                       side * (outer_w - wall) - 0.1,
                       outer_h_bottom * 0.3])
                vent_slots(vent_count, vent_slot_l, vent_slot_w, wall);
        }
    }
    
    // ---- PROWADNICE SZYNOWE DLA WEMOS D1 MINI ----
    // Dwie szyny po bokach trzymające PCB 1.6mm
    translate([wemos_x, wemos_y - 1.5, wemos_z])
        pcb_rail(wemos_l, wemos_pcb, 1.5);
    translate([wemos_x, wemos_y + wemos_w + 0.3, wemos_z])
        mirror([0, 1, 0])
            pcb_rail(wemos_l, wemos_pcb, 1.5);
    
    // ---- KANAŁ NA PRZEWODY ----
    // Pionowy kanał łączący sekcję Wemos z sekcją PN532 (góra)
    // (zostawiony jako wolna przestrzeń — nie musi być osobnym elementem)
}

// ============================================================
// GÓRNA POŁOWA OBUDOWY (TOP / POKRYWA)
// ============================================================

// Chamfered standoff — faza 45° u podstawy dla drukowalności
module standoff_chamfered(h, outer_d, inner_d, chamfer = 0.8) {
    difference() {
        union() {
            // Faza 45° (stożek u podstawy)
            cylinder(d1 = outer_d + 2*chamfer, d2 = outer_d, h = chamfer, $fn = 24);
            // Główny cylinder
            translate([0, 0, chamfer - 0.01])
                cylinder(d = outer_d, h = h - chamfer + 0.01, $fn = 24);
        }
        // Otwór na śrubę
        translate([0, 0, -0.1])
            cylinder(d = inner_d, h = h + 0.2, $fn = 24);
    }
}

// Przejście schodkowe (chamfer) między strefą grubą a cienką NFC
nfc_chamfer = 0.8; // mm — łagodne przejście

module top_case() {
    nfc_zone_x = pn532_x - 1;  // 1mm margines wokół anteny
    nfc_zone_y = pn532_y - 1;
    nfc_zone_l = pn532_l + 2;
    nfc_zone_w = pn532_w + 2;
    
    difference() {
        union() {
            // --- Zewnętrzna skorupa z cienkim sufitem (nfc_wall) ---
            difference() {
                rounded_box([outer_l, outer_w, outer_h_top], corner_r);

                // Wydrążenie — sufit = nfc_wall (cienki)
                translate([wall, wall, -0.1])
                    rounded_box([outer_l - 2*wall,
                                 outer_w - 2*wall,
                                 outer_h_top - nfc_wall + 0.1],
                                max(corner_r - wall, 0.5));
            }
            
            // --- Dogrubienie sufitu POZA strefą NFC do pełnego wall ---
            difference() {
                // Pełna grubość — dodajemy wall - nfc_wall od wewnątrz
                translate([wall, wall, outer_h_top - wall])
                    rounded_box([outer_l - 2*wall,
                                 outer_w - 2*wall,
                                 wall - nfc_wall],
                                max(corner_r - wall, 0.5));
                // Wycinamy strefę NFC — tu zostaje cienkie nfc_wall
                translate([nfc_zone_x, nfc_zone_y, outer_h_top - wall - 0.1])
                    cube([nfc_zone_l, nfc_zone_w, wall + 0.2]);
            }
            
            // --- Chamfer: łagodne przejście gruba→cienka ścianka ---
            // Rampa 45° wokół strefy NFC od wewnątrz
            difference() {
                translate([nfc_zone_x - nfc_chamfer, nfc_zone_y - nfc_chamfer,
                           outer_h_top - wall])
                    cube([nfc_zone_l + 2*nfc_chamfer, nfc_zone_w + 2*nfc_chamfer,
                          wall - nfc_wall]);
                // Wytnij wnętrze strefy
                translate([nfc_zone_x, nfc_zone_y, outer_h_top - wall - 0.1])
                    cube([nfc_zone_l, nfc_zone_w, wall + 0.2]);
                // Wytnij to co poza obudową (clip to inner walls)
                translate([0, 0, -0.1])
                    difference() {
                        cube([outer_l + 1, outer_w + 1, outer_h_top + 1]);
                        translate([wall, wall, 0])
                            rounded_box([outer_l - 2*wall,
                                         outer_w - 2*wall,
                                         outer_h_top + 2],
                                        max(corner_r - wall, 0.5));
                    }
            }
        }
        
        // ---- WYCIĘCIE na lip z dolnej połowy (z lekkim fazowaniem) ----
        translate([wall - lip_thick - tol/2,
                   wall - lip_thick - tol/2,
                   -0.1])
            rounded_box([outer_l - 2*(wall - lip_thick) + tol,
                         outer_w - 2*(wall - lip_thick) + tol,
                         lip_h + 0.1], corner_r);
        // Faza wejściowa lip — ułatwia zamknięcie
        translate([wall - lip_thick - tol/2 - 0.3,
                   wall - lip_thick - tol/2 - 0.3,
                   lip_h - 0.1])
            difference() {
                rounded_box([outer_l - 2*(wall - lip_thick) + tol + 0.6,
                             outer_w - 2*(wall - lip_thick) + tol + 0.6,
                             0.5], corner_r + 0.3);
                translate([0.3, 0.3, -0.1])
                    rounded_box([outer_l - 2*(wall - lip_thick) + tol,
                                 outer_w - 2*(wall - lip_thick) + tol,
                                 0.7], corner_r);
            }
        
        // ---- SYMBOL NFC — grawerowany w powierzchni ----
        translate([pn532_x + pn532_l/2, pn532_y + pn532_w/2, outer_h_top - 0.4])
            nfc_symbol(15);
    }
    
    // ---- SŁUPKI MONTAŻOWE PN532 — wiszą z sufitu w dół ----
    pn532_holes = [
        [pn532_hole_offset, pn532_hole_offset],
        [pn532_l - pn532_hole_offset, pn532_hole_offset],
        [pn532_hole_offset, pn532_w - pn532_hole_offset],
        [pn532_l - pn532_hole_offset, pn532_w - pn532_hole_offset]
    ];
    
    for (pos = pn532_holes) {
        // Słupki rosną z sufitu (outer_h_top - nfc_wall) w dół
        // mirror Z aby chamfer był przy suficie (mocniejsze połączenie)
        translate([pn532_x + pos[0], pn532_y + pos[1], outer_h_top - nfc_wall])
            mirror([0, 0, 1])
                standoff_chamfered(standoff_h, standoff_d, screw_d);
    }
    
    // ---- SYMBOL NFC NA ZEWNĄTRZ (grawerowanie w powierzchni) ----
    // Symbol jest wgłębiony (0.4mm) zamiast wypukłego —
    // dzięki temu powierzchnia NFC jest idealnie płaska do druku
}

// ============================================================
// WIZUALIZACJA PCB (tylko podgląd — nie eksportować!)
// ============================================================

module wemos_pcb_preview() {
    color("green", 0.5)
        cube([wemos_l, wemos_w, wemos_pcb]);
    // USB connector
    color("silver", 0.7)
        translate([-2, (wemos_w - 8)/2, wemos_pcb])
            cube([6, 8, 3]);
    // ESP-12F shield
    color("silver", 0.4)
        translate([wemos_l - 16, (wemos_w - 13)/2, wemos_pcb])
            cube([16, 13, 2.5]);
}

module pn532_pcb_preview() {
    color("blue", 0.5)
        cube([pn532_l, pn532_w, pn532_pcb]);
    // Antenna trace (frame)
    color("white", 0.3)
        difference() {
            translate([2, 2, pn532_pcb]) cube([pn532_l - 4, pn532_w - 4, 0.3]);
            translate([5, 5, pn532_pcb - 0.1]) cube([pn532_l - 10, pn532_w - 10, 0.5]);
        }
}

// ============================================================
// RENDEROWANIE CZĘŚCI
// ============================================================

if (part == "bottom") {
    bottom_case();
}
else if (part == "top") {
    // Pokrywa — odwrócona do druku:
    //   - Płaska górna ścianka (NFC) leży na stole (Z=0)
    //   - Słupki montażowe rosną do góry = bez supportów
    //   - Otwarta strona (lip) na górze = brak nawisów
    translate([0, outer_w, outer_h_top])
        rotate([180, 0, 0])
            top_case();
}
else if (part == "assembled") {
    // Złożony widok
    bottom_case();
    translate([0, 0, outer_h_bottom])
        top_case();
    
    // Podgląd PCB
    translate([wemos_x, wemos_y, wemos_z])
        wemos_pcb_preview();
    translate([pn532_x, pn532_y, 
               outer_h_bottom + wall + standoff_h])
        pn532_pcb_preview();
}
else if (part == "exploded") {
    // Widok eksplodowany
    bottom_case();
    translate([0, 0, outer_h_bottom + 20]) // 20mm separacji
        top_case();
    
    // PCB podgląd
    translate([wemos_x, wemos_y, wemos_z])
        wemos_pcb_preview();
    translate([pn532_x, pn532_y, 
               outer_h_bottom + 20 + wall + standoff_h])
        pn532_pcb_preview();
}

// ============================================================
// INFORMACJE DEBUGOWE
// ============================================================
echo("=== WYMIARY OBUDOWY ===");
echo(str("Zewnętrzne (LxWxH): ", outer_l, " x ", outer_w, " x ", outer_h, " mm"));
echo(str("Wewnętrzne (LxW): ", inner_l, " x ", inner_w, " mm"));
echo(str("Dół (H): ", outer_h_bottom, " mm"));
echo(str("Góra (H): ", outer_h_top, " mm"));
echo(str("Ścianka NFC: ", nfc_wall, " mm"));
