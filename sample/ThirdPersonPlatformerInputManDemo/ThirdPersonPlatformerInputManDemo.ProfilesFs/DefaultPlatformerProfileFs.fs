namespace ThirdPersonPlatformerInputManDemo

open System.Collections.Generic
open InputMan.Core
open Stride.Input
open type InputMan.Core.Bind
open type InputMan.StrideConn.StrideKeys

type DefaultPlatformerProfileFs =
    static member Jump = ActionId "Jump"
    static member LookLock = ActionId "LookLock"
    static member LookUnlock = ActionId "LookUnlock"
    static member MoveX = AxisId "MoveX"
    static member MoveY = AxisId "MoveY"
    static member LookStickX = AxisId "LookStickX"
    static member LookStickY = AxisId "LookStickY"
    static member LookMouseX = AxisId "LookMouseX"
    static member LookMouseY = AxisId "LookMouseY"
    static member Move = Axis2Id "Move"
    static member LookStick = Axis2Id "LookStick"
    static member LookMouse = Axis2Id "LookMouse"

    static member Create() : InputProfile =
        let S = DefaultPlatformerProfileFs // Short alias for internal references

        let bindings = [
            // Keyboard/Mouse
            yield ButtonAxis(K Keys.A, S.MoveX, -1.0f)
            yield ButtonAxis(K Keys.D, S.MoveX,  1.0f) 
            yield ButtonAxis(K Keys.S, S.MoveY, -1.0f) 
            yield ButtonAxis(K Keys.W, S.MoveY,  1.0f) 
            yield Action(K Keys.Space, S.Jump, ButtonEdge.Pressed) 
            yield DeltaAxis(MouseDeltaX, S.LookMouseX, 1.0f) 
            yield DeltaAxis(MouseDeltaY, S.LookMouseY, 1.0f) 
            yield Action(M MouseButton.Left, S.LookLock, ButtonEdge.Down) 
            yield Action(K Keys.Escape, S.LookUnlock, ButtonEdge.Pressed) 

            // Gamepad
            for i in 0uy .. 3uy do 
                yield Axis(PadLeftX i,  S.MoveX, 1.0f) 
                yield Axis(PadLeftY i,  S.MoveY, 1.0f) 
                yield Axis(PadRightX i, S.LookStickX, 1.0f) 
                yield Axis(PadRightY i, S.LookStickY, 1.0f) 
                yield Action(PadBtn(i, GamePadButton.A), S.Jump, ButtonEdge.Pressed) 
        ]

        let gameplay = ActionMapDefinition(Id = ActionMapId "Gameplay", Priority = 10, CanConsume = false) 
        gameplay.Bindings <- ResizeArray bindings 

        InputProfile(
            Maps = Dictionary(dict ["Gameplay", gameplay]), 
            Axis2 = Dictionary(dict [
                "Move",      Axis2Definition(Id = S.Move,      X = S.MoveX,      Y = S.MoveY) 
                "LookStick", Axis2Definition(Id = S.LookStick, X = S.LookStickX, Y = S.LookStickY) 
                "LookMouse", Axis2Definition(Id = S.LookMouse, X = S.LookMouseX, Y = S.LookMouseY) 
            ])
        )