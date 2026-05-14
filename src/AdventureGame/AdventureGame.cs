namespace AdventureGame;

public class AdventureGame
{
    // ------------------------------------------------------------
    // COMANDOS DEL JUEGO
    // Estos valores son las teclas que el jugador puede usar.
    // ------------------------------------------------------------
    public readonly string GO_NORTH = "W";
    public readonly string GO_SOUTH = "S";
    public readonly string GO_EAST = "D";
    public readonly string GO_WEST = "A";
    public readonly string GET_LAMP = "L";
    public readonly string GET_KEY = "K";
    public readonly string OPEN_CHEST = "O";
    public readonly string QUIT = "Q";

    // ------------------------------------------------------------
    // OBJETOS PRINCIPALES DEL JUEGO
    // adventurer representa al jugador.
    // dungeon es una matriz de cuartos cargada desde DungeonTemplate.txt.
    // ------------------------------------------------------------
    private Adventurer adventurer;
    private Room[,] dungeon;

    // Posicion actual del jugador dentro del dungeon.
    private int aRow;
    private int aCol;

    // Estados principales del juego.
    private bool isChestOpen;
    private bool hasPlayerQuit;
    private bool isAdventureAlive;
    private bool playerWon;

    // Se usa para la mecanica original del Grue en cuartos oscuros.
    // Guarda la direccion contraria a la ultima direccion usada.
    private string lastDirection;

    // Caracter que representa una pared en el archivo DungeonTemplate.txt.
    private const char Wall = '#';

    // Posicion de la salida.
    private int exitRow;
    private int exitCol;

    // ------------------------------------------------------------
    // NUEVA MECANICA DEL GRUE
    // Estas variables guardan la posicion actual del Grue.
    // El Grue empieza en una posicion cargada desde el archivo txt.
    // Despues de abrir el cofre, el Grue aparece en el mapa y persigue
    // al jugador.
    // ------------------------------------------------------------
    private int grueRow;
    private int grueCol;

    // Posiciones de los items y el cofre.
    private int lampRow;
    private int lampCol;

    private int keyRow;
    private int keyCol;

    private int chestRow;
    private int chestCol;

    // ------------------------------------------------------------
    // NUEVA MECANICA DEL GRUE
    // Esta variable decide si el Grue debe moverse en el turno actual.
    // Se activa cuando:
    // - El jugador se mueve.
    // - El jugador choca con una pared despues de abrir el cofre.
    // - El jugador escribe una tecla incorrecta despues de abrir el cofre.
    // - El jugador intenta repetir una accion que ya no puede hacer.
    // ------------------------------------------------------------
    private bool grueShouldMove;

    public AdventureGame()
    {
    }

    public void Start()
    {
        Init();

        ClearScreen();
        ShowGameStartScreen();

        string input;

        // Loop principal del juego.
        // Se repite hasta que el jugador gane, pierda o salga.
        do
        {
            ClearScreen();

            ShowScene();

            ShowInputOptions();
            input = GetInput();

            ProcessInput(input);
            UpdateGameState();

            if (!IsGameOver())
            {
                Console.WriteLine();
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }
        while (!IsGameOver());

        ShowGameOverScreen();
    }

    private void ClearScreen()
    {
        Console.Clear();

        // Limpia mejor el historial visual del terminal y mueve el cursor al inicio.
        // Esto evita que el mapa se quede repetido cuando se le vuelve a dar Play.
        Console.Write("\u001b[2J\u001b[3J\u001b[H");
    }

    private void Init()
    {
        adventurer = new Adventurer();

        // ------------------------------------------------------------
        // CARGA DEL DUNGEON DESDE ARCHIVO
        // ------------------------------------------------------------
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "res",
            "DungeonTemplate.txt"
        );

        LoadDungeon(Path.GetFullPath(path));

        // El jugador empieza donde esta la lampara.
        aRow = lampRow;
        aCol = lampCol;

        // Estado inicial del juego.
        isChestOpen = false;
        hasPlayerQuit = false;
        isAdventureAlive = true;
        playerWon = false;
        grueShouldMove = false;

        lastDirection = string.Empty;
    }

    private void ShowGameStartScreen()
    {
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine("|        Welcome to Oscar's Adventure Game!      |");
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine();
        Console.WriteLine("Find the treasure, escape the dungeon, and avoid the Grue.");
        Console.WriteLine();
        Console.WriteLine("Controls:");
        Console.WriteLine("W = North | S = South | A = West | D = East");
        Console.WriteLine("L = Get Lamp | K = Get Key | O = Open Chest | Q = Quit");
        Console.WriteLine();
        Console.WriteLine("Press Enter to start...");
        Console.ReadLine();
    }

    private void ShowScene()
    {
        ShowMap();

        Room r = dungeon[aRow, aCol];

        // Si el jugador tiene la lampara o el cuarto esta iluminado,
        // se muestra la descripcion normal.
        // Si no, solo se dice que esta oscuro.
        if (adventurer.HasLamp() || r.IsLit())
        {
            Console.WriteLine(r.GetDescription());
        }
        else
        {
            Console.WriteLine("This room is pitch black!");
        }

        // Espacio visual para que la descripcion no quede pegada a las teclas.
        Console.WriteLine();
    }

    private void ShowMap()
    {
        Console.WriteLine();
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine("|                  DUNGEON MAP                   |");
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine();

        for (int row = 0; row < dungeon.GetLength(0); row++)
        {
            Console.Write("   ");

            for (int col = 0; col < dungeon.GetLength(1); col++)
            {
                if (dungeon[row, col] == null)
                {
                    Console.Write("██");
                }
                else if (row == aRow && col == aCol)
                {
                    Console.Write("A ");
                }
                // ------------------------------------------------------------
                // NUEVA MECANICA DEL GRUE
                // El Grue solamente se muestra en el mapa despues de abrir
                // el cofre. Antes de eso, esta oculto.
                // ------------------------------------------------------------
                else if (isChestOpen && row == grueRow && col == grueCol)
                {
                    Console.Write("G ");
                }
                else if (row == exitRow && col == exitCol)
                {
                    Console.Write("E ");
                }
                else if (dungeon[row, col].HasLamp())
                {
                    Console.Write("L ");
                }
                else if (dungeon[row, col].HasKey())
                {
                    Console.Write("K ");
                }
                else if (dungeon[row, col].HasChest())
                {
                    Console.Write("C ");
                }
                else
                {
                    Console.Write("· ");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine("| Legend                                         |");
        Console.WriteLine("| A = Adventurer   E = Exit       G = Grue       |");
        Console.WriteLine("| L = Lamp         K = Key        C = Chest      |");
        Console.WriteLine("| ██ = Wall        · = Path                      |");
        Console.WriteLine("+------------------------------------------------+");
        Console.WriteLine();
    }

    private void ShowInputOptions()
    {
        string options = ""
        + $"GO NORTH [{GO_NORTH}] | GO EAST [{GO_EAST}] | GET LAMP [{GET_LAMP}] | OPEN CHEST [{OPEN_CHEST}]\n"
        + $"GO SOUTH [{GO_SOUTH}] | GO WEST [{GO_WEST}] | GET KEY  [{GET_KEY}] | QUIT       [{QUIT}]\n"
        + $"> ";

        Console.Write(options);
    }

    private string GetInput()
    {
        return Console.ReadLine()!.ToUpper();
    }

    private bool IsValidInput(string input)
    {
        string[] validInputs =
        {
            GO_NORTH,
            GO_SOUTH,
            GO_EAST,
            GO_WEST,
            GET_LAMP,
            GET_KEY,
            OPEN_CHEST,
            QUIT
        };

        return validInputs.Contains(input);
    }

    private void ProcessInput(string input)
    {
        // Cada turno empieza con esta variable en false.
        // Esto significa que el Grue no se movera automaticamente.
        // Mas abajo, la cambiamos a true si el jugador hace algo que cuenta como turno.
        grueShouldMove = false;

        if (!IsValidInput(input))
        {
            Console.WriteLine("ERROR: Invalid input. The Grue heard your mistake!");

            // ------------------------------------------------------------
            // NUEVA MECANICA DEL GRUE
            // Si el cofre ya fue abierto y el jugador comete un error,
            // el Grue tambien gana un turno para moverse.
            // ------------------------------------------------------------
            if (isChestOpen)
            {
                grueShouldMove = true;
            }

            return;
        }

        Room r = dungeon[aRow, aCol];

        // ------------------------------------------------------------
        // MECANICA ORIGINAL DEL GRUE
        // si el jugador no tiene lampara y esta en un cuarto oscuro,
        // el Grue puede devorarlo si no regresa por donde vino.
        // ------------------------------------------------------------
        if (!adventurer.HasLamp() && !r.IsLit() && input != lastDirection)
        {
            Console.WriteLine("You got eaten alive by the Grue!");
            isAdventureAlive = false;
        }
        else if (input == GO_NORTH)
        {
            GoNorth(r);
        }
        else if (input == GO_SOUTH)
        {
            GoSouth(r);
        }
        else if (input == GO_EAST)
        {
            GoEast(r);
        }
        else if (input == GO_WEST)
        {
            GoWest(r);
        }
        else if (input == GET_LAMP)
        {
            GetLamp(r);
        }
        else if (input == GET_KEY)
        {
            GetKey(r);
        }
        else if (input == OPEN_CHEST)
        {
            OpenChest(r);
        }
        else
        {
            Quit();
        }
    }

    private void UpdateGameState()
    {
        if (!isAdventureAlive || hasPlayerQuit)
            return;

        // ------------------------------------------------------------
        // NUEVA MECANICA DEL GRUE
        // Antes el juego terminaba al abrir el cofre.
        // Ahora abrir el cofre solo despierta al Grue.
        // - El juego termina cuando:
        // - El jugador llega al exit con el cofre abierto.
        // - El Grue alcanza al jugador.
        // - El jugador sale del juego.
        // ------------------------------------------------------------
        if (isChestOpen)
        {
            // Condicion de victoria:
            // El jugador gana solo si llega al exit despues de abrir el cofre.
            if (aRow == exitRow && aCol == exitCol)
            {
                Console.WriteLine();
                Console.WriteLine("You escaped the dungeon with the treasure! YOU WIN!");
                playerWon = true;
                return;
            }

            // Si el Grue ya esta en el mismo cuarto que el jugador, pierde.
            if (aRow == grueRow && aCol == grueCol)
            {
                Console.WriteLine();
                Console.WriteLine("The Grue is in the same room as you. You were eaten!");
                isAdventureAlive = false;
                return;
            }

            // Si este turno cuenta como movimiento del Grue, se mueve.
            if (grueShouldMove)
            {
                MoveGrue();

                // Despues de moverse, verificamos otra vez si alcanzo al jugador.
                if (aRow == grueRow && aCol == grueCol)
                {
                    Console.WriteLine();
                    Console.WriteLine("The Grue caught you! You were eaten!");
                    isAdventureAlive = false;
                    return;
                }

                Console.WriteLine("You hear the Grue moving closer...");
            }
        }
    }

    private void MoveGrue()
    {
        // ------------------------------------------------------------
        // NUEVA MECANICA DEL GRUE - PERSECUCION INTELIGENTE
        //
        // Este metodo hace que el Grue persiga al jugador.
        // Para eso usa BFS, que significa Breadth First Search.
        //
        // 1. El Grue empieza desde su posicion actual.
        // 2. Revisa los cuartos vecinos que son caminables.
        // 3. Sigue buscando hasta encontrar la posicion del jugador.
        // 4. Reconstruye el camino.
        // 5. El Grue se mueve solo un paso en ese camino.
        //
        // Esto es importante porque el Grue no atraviesa paredes.
        // Se mueve usando los pasillos reales del dungeon.
        // ------------------------------------------------------------

        Queue<(int row, int col)> queue = new();
        Dictionary<(int row, int col), (int row, int col)> previous = new();
        HashSet<(int row, int col)> visited = new();

        (int row, int col) start = (grueRow, grueCol);
        (int row, int col) target = (aRow, aCol);

        queue.Enqueue(start);
        visited.Add(start);

        bool foundPlayer = false;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                foundPlayer = true;
                break;
            }

            List<(int row, int col)> neighbors = GetTraversableNeighbors(current.row, current.col);

            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    previous[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!foundPlayer)
        {
            return;
        }

        // Aqui reconstruimos el camino desde el jugador hacia el Grue.
        // Luego escogemos el primer paso que debe tomar el Grue.
        (int row, int col) step = target;

        while (previous.ContainsKey(step) && previous[step] != start)
        {
            step = previous[step];
        }

        grueRow = step.row;
        grueCol = step.col;
    }

    private List<(int row, int col)> GetTraversableNeighbors(int row, int col)
    {
        // Este metodo devuelve todos los cuartos vecinos donde el Grue puede caminar.
        // No incluye paredes.
        List<(int row, int col)> neighbors = new();

        TryAddMove(neighbors, row - 1, col);
        TryAddMove(neighbors, row + 1, col);
        TryAddMove(neighbors, row, col - 1);
        TryAddMove(neighbors, row, col + 1);

        return neighbors;
    }

    private void TryAddMove(List<(int row, int col)> moves, int row, int col)
    {
        // Solo agrega el movimiento si la posicion es caminable.
        if (IsTraversable(row, col))
        {
            moves.Add((row, col));
        }
    }

    private void MakeGrueMoveIfAwake()
    {
        // ------------------------------------------------------------
        // NUEVA MECANICA DEL GRUE
        // Este metodo se usa cuando el jugador hace una accion que no avanza,
        // como chocar con pared, repetir una accion o usar una tecla incorrecta.
        // Si el cofre ya esta abierto, esa accion tambien le da turno al Grue.
        // ------------------------------------------------------------
        if (isChestOpen)
        {
            grueShouldMove = true;
        }
    }

    private bool IsGameOver()
    {
        // El juego termina si el jugador gana, sale o muere.
        return playerWon || hasPlayerQuit || !isAdventureAlive;
    }

    private void ShowGameOverScreen()
    {
        Console.WriteLine();
        Console.WriteLine("Game Over!");
    }

    private void GoNorth(Room r)
    {
        if (r.HasNorth())
        {
            aRow -= 1;
            lastDirection = GO_SOUTH;
            grueShouldMove = true;
        }
        else
        {
            Console.WriteLine("You cannot go north!");
            MakeGrueMoveIfAwake();
        }
    }

    private void GoSouth(Room r)
    {
        if (r.HasSouth())
        {
            aRow += 1;
            lastDirection = GO_NORTH;
            grueShouldMove = true;
        }
        else
        {
            Console.WriteLine("You cannot go south!");
            MakeGrueMoveIfAwake();
        }
    }

    private void GoEast(Room r)
    {
        if (r.HasEast())
        {
            aCol += 1;
            lastDirection = GO_WEST;
            grueShouldMove = true;
        }
        else
        {
            Console.WriteLine("You cannot go east!");
            MakeGrueMoveIfAwake();
        }
    }

    private void GoWest(Room r)
    {
        if (r.HasWest())
        {
            aCol -= 1;
            lastDirection = GO_EAST;
            grueShouldMove = true;
        }
        else
        {
            Console.WriteLine("You cannot go west!");
            MakeGrueMoveIfAwake();
        }
    }

    private void GetLamp(Room r)
    {
        if (r.HasLamp())
        {
            Console.WriteLine("You got the lamp!");
            adventurer.SetLamp(true);
            r.SetLamp(false);
        }
        else
        {
            if (adventurer.HasLamp())
            {
                Console.WriteLine("You cannot perform this option anymore.");
            }
            else
            {
                Console.WriteLine("There is no lamp in this room.");
            }

            MakeGrueMoveIfAwake();
        }
    }

    private void GetKey(Room r)
    {
        if (r.HasKey())
        {
            Console.WriteLine("You got the key!");
            adventurer.SetKey(true);
            r.SetKey(false);
        }
        else
        {
            if (adventurer.HasKey())
            {
                Console.WriteLine("You cannot perform this option anymore.");
            }
            else
            {
                Console.WriteLine("There is no key in this room.");
            }

            MakeGrueMoveIfAwake();
        }
    }

    private void OpenChest(Room r)
    {
        if (r.HasChest())
        {
            if (adventurer.HasKey())
            {
                if (!isChestOpen)
                {
                    // ------------------------------------------------------------
                    // NUEVA MECANICA DEL GRUE
                    // En vez de terminar el juego al abrir el cofre, ahora se despierta al Grue.
                    // ------------------------------------------------------------
                    Console.WriteLine("You got the treasure! The Grue has awakened and is now chasing you!");
                    isChestOpen = true;
                    r.SetChest(false);
                }
                else
                {
                    Console.WriteLine("You cannot perform this option anymore.");
                    MakeGrueMoveIfAwake();
                }
            }
            else
            {
                Console.WriteLine("You do not have the key!");
                MakeGrueMoveIfAwake();
            }
        }
        else
        {
            if (isChestOpen)
            {
                Console.WriteLine("You cannot perform this option anymore.");
            }
            else
            {
                Console.WriteLine("There is no chest in this room.");
            }

            MakeGrueMoveIfAwake();
        }
    }

    private void Quit()
    {
        Console.WriteLine("You quit the game!");
        hasPlayerQuit = true;
    }

    private void LoadDungeon(string filePath)
    {
        // ------------------------------------------------------------
        // CARGA DEL DUNGEON
        // Este metodo lee DungeonTemplate.txt.
        // El archivo contiene:
        // - Cantidad de filas y columnas.
        // - Posicion del exit.
        // - Posicion de la lampara.
        // - Posicion de la llave.
        // - Posicion del cofre.
        // - Posicion inicial del Grue.
        // - El mapa con paredes y espacios caminables.
        // - Las descripciones de cada cuarto.
        // ------------------------------------------------------------

        string[] lines = File.ReadAllLines(filePath);

        int rows = int.Parse(lines[0]);
        int cols = int.Parse(lines[1]);

        exitRow = int.Parse(lines[2]);
        exitCol = int.Parse(lines[3]);

        lampRow = int.Parse(lines[4]);
        lampCol = int.Parse(lines[5]);

        keyRow = int.Parse(lines[6]);
        keyCol = int.Parse(lines[7]);

        chestRow = int.Parse(lines[8]);
        chestCol = int.Parse(lines[9]);

        // NUEVA MECANICA DEL GRUE:
        // Aqui se lee la posicion inicial del Grue desde el archivo txt.
        grueRow = int.Parse(lines[10]);
        grueCol = int.Parse(lines[11]);

        int layoutStart = 12;
        int descriptionsStart = layoutStart + rows;

        if (lines.Length < descriptionsStart)
            throw new FormatException("File does not contain enough layout rows.");

        dungeon = new Room[rows, cols];
        List<(int row, int col)> traversableTiles = new();

        for (int row = 0; row < rows; row++)
        {
            string layoutLine = lines[layoutStart + row];

            if (layoutLine.Length != cols)
                throw new FormatException($"Layout row {row} must contain exactly {cols} characters.");

            for (int col = 0; col < cols; col++)
            {
                if (layoutLine[col] != Wall)
                {
                    dungeon[row, col] = new Room();
                    traversableTiles.Add((row, col));
                }
            }
        }

        int descriptionCount = lines.Length - descriptionsStart;

        if (descriptionCount != traversableTiles.Count)
        {
            throw new FormatException(
                $"Description count ({descriptionCount}) must match traversable tile count ({traversableTiles.Count})."
            );
        }

        for (int i = 0; i < traversableTiles.Count; i++)
        {
            string[] parts = lines[descriptionsStart + i].Split('|', 2);

            if (parts.Length != 2)
                throw new FormatException($"Invalid room description line: {lines[descriptionsStart + i]}");

            bool isLit = parts[0] switch
            {
                "1" => true,
                "0" => false,
                _ => throw new FormatException("Room lit value must be 1 or 0.")
            };

            string description = parts[1];

            var (row, col) = traversableTiles[i];
            Room room = dungeon[row, col];

            room.SetLit(isLit);
            room.SetDescription(description);

            room.SetLamp(row == lampRow && col == lampCol);
            room.SetKey(row == keyRow && col == keyCol);
            room.SetChest(row == chestRow && col == chestCol);

            room.SetNorth(IsTraversable(row - 1, col));
            room.SetSouth(IsTraversable(row + 1, col));
            room.SetEast(IsTraversable(row, col + 1));
            room.SetWest(IsTraversable(row, col - 1));
        }

        ValidateTraversableTile(exitRow, exitCol, "exit");
        ValidateTraversableTile(lampRow, lampCol, "lamp");
        ValidateTraversableTile(keyRow, keyCol, "key");
        ValidateTraversableTile(chestRow, chestCol, "chest");
        ValidateTraversableTile(grueRow, grueCol, "grue");
    }

    private bool IsTraversable(int row, int col)
    {
        return row >= 0 &&
               row < dungeon.GetLength(0) &&
               col >= 0 &&
               col < dungeon.GetLength(1) &&
               dungeon[row, col] != null;
    }

    private void ValidateTraversableTile(int row, int col, string name)
    {
        if (!IsTraversable(row, col))
            throw new FormatException($"The {name} position must be on a traversable tile.");
    }
}