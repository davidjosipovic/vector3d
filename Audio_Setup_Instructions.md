# Audio Manager Setup Instructions

## 1. Kreiranje AudioManager GameObject-a

### U Main Menu sceni:
1. **Kreiraj prazan GameObject** u hijerarhiji
2. **Imenuj ga "AudioManager"**
3. **Dodaj AudioManager script** na ovaj objekat
4. **Pozicioniraj ga na (0, 0, 0)**

## 2. Dodavanje Audio Klipova

### Potrebne muzike:
- **Main Menu Music** - opuštena/atmosferska muzika za glavni meni
- **Gameplay Music** - dinamična/akciona muzika za levelе

### Dodavanje klipova:
1. **Uvezi audio fajlove** u `Assets/Audio/` folder
2. **Selektuj AudioManager** u hijerarhiji
3. **U Inspector-u, dodeli klipove:**
   - Main Menu Music → Dodeli main menu audio clip
   - Gameplay Music → Dodeli gameplay audio clip

## 3. Podešavanje Audio Source-a

AudioManager će automatski kreirati AudioSource komponente, ali možeš ih i manuelno dodati:

### Music Source:
- **Loop**: ✅ Uključeno
- **Play On Awake**: ❌ Isključeno
- **Volume**: 0.7 (podešava se u AudioManager script-u)

### SFX Source:
- **Loop**: ❌ Isključeno
- **Play On Awake**: ❌ Isključeno
- **Volume**: 1.0

## 4. Testiranje

### Debug kontrole (u Play mode-u):
- **F1** - Pokreni main menu muziku
- **F2** - Pokreni gameplay muziku
- **F3** - Zaustavi muziku
- **F4** - Pauza/Resume muziku

### Automatsko ponašanje:
- **Main Menu** - Muzika se automatski pokreće kada se učita glavni meni
- **Level Start** - Gameplay muzika se pokreće kada PlayerController startuje
- **Scene Transition** - Muzika se automatski menja sa fade effect-om

## 5. Podešavanje Volume-a

U AudioManager Inspector-u:
- **Music Volume**: 0-1 (preporučeno 0.7)
- **SFX Volume**: 0-1 (preporučeno 1.0)
- **Fade Transitions**: ✅ Za glatke prelaze između muzika
- **Fade Duration**: 1s (brzina fade effect-a)

## 6. Dodatne Funkcionalnosti

### Programatska kontrola:
```csharp
// Promeni main menu muziku
AudioManager.Instance.PlayMainMenuMusic();

// Promeni gameplay muziku
AudioManager.Instance.PlayGameplayMusic();

// Podesi volume
AudioManager.Instance.SetMusicVolume(0.5f);

// Zaustavi muziku
AudioManager.Instance.StopMusic();
```

### SFX podrška:
```csharp
// Pusti sound effect
AudioManager.Instance.PlaySFX(jumpSoundClip);
```

## 7. Troubleshooting

### Ako muzika ne radi:
1. **Provjeri da li je AudioManager u sceni**
2. **Provjeri da li su audio klipovi dodeljeni**
3. **Provjeri volume settings**
4. **Provjeri da li je audio uključen u projektu**

### Ako nema fade transition-a:
1. **Uključi "Fade Transitions" u AudioManager**
2. **Podesi "Fade Duration" na željenu vrednost**

### Performance:
- AudioManager koristi **DontDestroyOnLoad** - ostaje kroz sve scene
- Singleton pattern - samo jedan AudioManager po game session-u
- Automatsko upravljanje AudioSource komponentama

## 8. Proširenja

Možeš dodati:
- **Više muzičkih tema** za različite nivoe
- **Volume sliders** u settings menu-u
- **SFX klipove** za skakanje, wall run, sliding, itd.
- **Ambient sounds** za različite scene
