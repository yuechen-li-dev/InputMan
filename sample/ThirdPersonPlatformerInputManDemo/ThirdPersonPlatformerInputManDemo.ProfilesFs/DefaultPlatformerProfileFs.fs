namespace ThirdPersonPlatformerInputManDemo

open System.Collections.Generic
open InputMan.Core
open Stride.Input
open type InputMan.Core.Bind
open type InputMan.StrideConn.StrideKeys

module DefaultPlatformerProfileFs =

    // --- IDs ---
    let jump       = ActionId "Jump"
    let lookLock   = ActionId "LookLock"
    let lookUnlock = ActionId "LookUnlock"

    let pause      = ActionId "Pause"
    let confirm    = ActionId "UIConfirm"
    let cancel     = ActionId "UICancel"

    let moveX      = AxisId "MoveX"
    let moveY      = AxisId "MoveY"

    let lookStickX = AxisId "LookStickX"
    let lookStickY = AxisId "LookStickY"

    let lookMouseX = AxisId "LookMouseX"
    let lookMouseY = AxisId "LookMouseY"

    let move       = Axis2Id "Move"
    let lookStick  = Axis2Id "LookStick"
    let lookMouse  = Axis2Id "LookMouse"

    // --- Helpers ---
    let axis2 (key: string) (id: Axis2Id) (x: AxisId) (y: AxisId) =
        key, Axis2Definition(Id = id, X = x, Y = y)

    let map (key: string) (def: ActionMapDefinition) =
        key, def

    // --- Gameplay binding groups ---
    let wasd : seq<Binding> =
        seq {
            yield ButtonAxis(K Keys.A, moveX, -1.0f)
            yield ButtonAxis(K Keys.D, moveX,  1.0f)
            yield ButtonAxis(K Keys.S, moveY, -1.0f)
            yield ButtonAxis(K Keys.W, moveY,  1.0f)
        }

    let gameplayActions : seq<Binding> =
        seq {
            yield Action(K Keys.Space, jump, ButtonEdge.Pressed)
            // Escape unlock belongs to gameplay because it changes camera lock state
            yield Action(K Keys.Escape, lookUnlock, ButtonEdge.Pressed)
        }

    let mouseLook : seq<Binding> =
        seq {
            yield DeltaAxis(MouseDeltaX, lookMouseX, 1.0f)
            yield DeltaAxis(MouseDeltaY, lookMouseY, 1.0f)
        }

    let mouseLock : seq<Binding> =
        seq {
            yield Action(M MouseButton.Left, lookLock, ButtonEdge.Down)
        }

    let gamepads : seq<Binding> =
        seq {
            for i in 0uy .. 3uy do
                yield Axis(PadLeftX i,  moveX, 1.0f)
                yield Axis(PadLeftY i,  moveY, 1.0f)
                yield Axis(PadRightX i, lookStickX, 1.0f)
                yield Axis(PadRightY i, lookStickY, 1.0f)
                yield Action(PadBtn(i, GamePadButton.A), jump, ButtonEdge.Pressed)
        }

    // --- UI binding groups ---
    // UI should consume to prevent gameplay leakage while menus are open.
    let uiNav : seq<Binding> =
        seq {
            // Pause toggle
            yield Action(K Keys.Escape, pause, ButtonEdge.Pressed, consume = ConsumeMode.All)
            yield Action(PadBtn(0uy, GamePadButton.Start), pause, ButtonEdge.Pressed, consume = ConsumeMode.All)
        }

    let uiConfirmCancel : seq<Binding> =
        seq {
            yield Action(K Keys.Enter, confirm, ButtonEdge.Pressed, consume = ConsumeMode.All)
            yield Action(PadBtn(0uy, GamePadButton.A), confirm, ButtonEdge.Pressed, consume = ConsumeMode.All)

            yield Action(K Keys.Back, cancel, ButtonEdge.Pressed, consume = ConsumeMode.All)
            yield Action(PadBtn(0uy, GamePadButton.B), cancel, ButtonEdge.Pressed, consume = ConsumeMode.All)
        }

    let Create () : InputProfile =

        let gameplayBindings =
            Seq.concat [ wasd; gameplayActions; mouseLook; mouseLock; gamepads ]
            |> ResizeArray

        let uiBindings =
            Seq.concat [ uiNav; uiConfirmCancel ]
            |> ResizeArray

        let gameplay =
            ActionMapDefinition(
                Id = ActionMapId "Gameplay",
                Priority = 10,
                CanConsume = false,
                Bindings = gameplayBindings
            )

        let ui =
            ActionMapDefinition(
                Id = ActionMapId "UI",
                Priority = 100,
                CanConsume = true,
                Bindings = uiBindings
            )

        InputProfile(
            Maps =
                [ map "Gameplay" gameplay
                  map "UI" ui ]
                |> dict
                |> Dictionary,

            Axis2 =
                [
                    axis2 "Move"      move      moveX      moveY
                    axis2 "LookStick" lookStick lookStickX lookStickY
                    axis2 "LookMouse" lookMouse lookMouseX lookMouseY
                ]
                |> dict
                |> Dictionary
        )
