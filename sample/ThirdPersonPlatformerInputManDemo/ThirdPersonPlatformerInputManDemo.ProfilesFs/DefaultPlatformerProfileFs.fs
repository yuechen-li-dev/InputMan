namespace ThirdPersonPlatformerInputManDemoFs

open System.Collections.Generic
open InputMan.Core
open Stride.Input
open type InputMan.Core.Bind
open type InputMan.StrideConn.StrideKeys

module DefaultPlatformerProfileFs =

    // IDs used by PlayerInput.cs (match the C# names)
    // --- Identifiers ---
    let Jump, LookLock, LookUnlock = ActionId "Jump", ActionId "LookLock", ActionId "LookUnlock"
    let MoveX, MoveY               = AxisId "MoveX", AxisId "MoveY"
    let LookStickX, LookStickY     = AxisId "LookStickX", AxisId "LookStickY"
    let LookMouseX, LookMouseY     = AxisId "LookMouseX", AxisId "LookMouseY"
    let Move, LookStick, LookMouse = Axis2Id "Move", Axis2Id "LookStick", Axis2Id "LookMouse"
    
    // Keymap
    let wasd moveX moveY =
        [
        ButtonAxis(K Keys.A, moveX, -1.0f)
        ButtonAxis(K Keys.D, moveX,  1.0f)
        ButtonAxis(K Keys.S, moveY, -1.0f)
        ButtonAxis(K Keys.W, moveY,  1.0f) 
        ]

    let mouseLook lookX lookY =
        [
        DeltaAxis(MouseDeltaX, lookX, 1.0f)
        DeltaAxis(MouseDeltaY, lookY, 1.0f) 
        ]

    let lockUnlock lockAct unlockAct =
        [
        Action(M MouseButton.Left,  lockAct,   ButtonEdge.Pressed)
        Action(K Keys.Escape,       unlockAct, ButtonEdge.Pressed) 
        ]

    let pad i moveX moveY lookX lookY jump =
        [
        Axis(PadLeftX i,  moveX, 1.0f)
        Axis(PadLeftY i,  moveY, 1.0f)
        Axis(PadRightX i, lookX, 1.0f)
        Axis(PadRightY i, lookY, 1.0f)
        Action(PadBtn(i, GamePadButton.A), jump, ButtonEdge.Pressed) 
        ]

    let profile =

        // Binding
        let kbMouseBindings =
            [
            yield! wasd MoveX MoveY
            yield  Action(K Keys.Space, Jump, ButtonEdge.Pressed)
            yield! mouseLook LookMouseX LookMouseY
            yield! lockUnlock LookLock LookUnlock 
            ]

        let gamepadBindings =
            [0uy..3uy] |> List.collect (fun i -> pad i MoveX MoveY LookStickX LookStickY Jump)

        let gameplay =
            ActionMapDefinition(
                Id = ActionMapId "Gameplay",
                Priority = 10,
                CanConsume = false,
                Bindings = ResizeArray(kbMouseBindings @ gamepadBindings)
            )

        InputProfile(
            Maps =
                Dictionary<string, ActionMapDefinition>(
                    dict [ "Gameplay", gameplay ]
                ),
            Axis2 =
                Dictionary<string, Axis2Definition>(
                    dict [
                        "Move",      Axis2Definition(Id = Move,      X = MoveX,      Y = MoveY)
                        "LookStick", Axis2Definition(Id = LookStick, X = LookStickX, Y = LookStickY)
                        "LookMouse", Axis2Definition(Id = LookMouse, X = LookMouseX, Y = LookMouseY)
                    ]
                )
        )