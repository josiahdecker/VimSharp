namespace ViEmu

    open System
    open System.Windows.Input
    open System.ComponentModel.Composition
    open Microsoft.VisualStudio
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    open Microsoft.VisualStudio.OLE.Interop
    open Microsoft.VisualStudio.TextManager.Interop
    open Microsoft.VisualStudio.Utilities

    open ViEmu.Interfaces

    [<Export(typeof<KeyProcessor>)>]
    [<ContentType("text")>]
    type ViKeyProcessor(context: IViContext, intellisenseMonitor: ViEmu.Monitors.IntellisenseMonitor)=
        inherit KeyProcessor()

        override this.IsInterestedInHandledEvents with get() = true
        
        ///Does not capture tab or escape keys
        override this.PreviewKeyDown args =
            match context.Mode with
                | Some(mode) -> 
                    mode.HandleKey context args
                    if not args.Handled then base.PreviewKeyDown args 
                | None -> base.PreviewKeyDown args

        ///escape key needs to be handled with an OleCommand in order to detect whether intellisense is
        ///active at the time. Tab key also handled in OleCommand so as to block further propogation. 
        override this.PreviewKeyUp args =
            match (args.Key, context.Mode) with
                |Key.Tab, Some(mode) ->
                    //command already handled by filter
                    ()
                |Key.Enter, Some(mode) ->
                    if not <| mode.HandleEnter context then
                        base.PreviewKeyUp args
                | _, _ ->     base.PreviewKeyUp args

        member this.EscapeCommandSent () = 
            if not <| intellisenseMonitor.IsIntellisenseActive() then
                context.Operations.ResetSelection()
                context.SetBaseMode()
            else
                intellisenseMonitor.DeactivateIntellisense()

        member this.TabCommandSent () =
            match context.Mode with
             | Some(mode) -> mode.HandleTab context
             | None -> false

        member this.BackTabCommandSent () =
            match context.Mode with
             | Some(mode) -> mode.HandleBackTab context
             | None -> false

    and CommandFilter(processor: ViKeyProcessor, viewAdapter: IVsTextView) =
        let mutable nextTarget: IOleCommandTarget option = None

        let (|Escape|Tab|BackTab|Other|) commandID =
            let commandID = uint32 commandID
            match commandID with
             | x when x = uint32 VSConstants.VSStd2KCmdID.CANCEL 
                    || x = uint32 VSConstants.VSStd2KCmdID.Cancel -> Escape
             | x when x = uint32 VSConstants.VSStd2KCmdID.TAB -> Tab
             | x when x = uint32 VSConstants.VSStd2KCmdID.BACKTAB -> BackTab
             | _ -> Other

        member this.NextTarget with get() = nextTarget
                                    and set(value) = nextTarget <- value 

        member this.Disconnect () =
            viewAdapter.RemoveCommandFilter(this)        

        interface IOleCommandTarget with
            member this.Exec(commandGroup, commandID, opt, pvaIn, pvaOut) =
                let blockPropogation: bool =
                    match commandGroup with
                     | x when x = VSConstants.VSStd2K ->
                        match commandID with
                         | Escape -> 
                            processor.EscapeCommandSent()
                            false
                         | Tab -> processor.TabCommandSent()
                         | BackTab -> processor.BackTabCommandSent()
                         | Other -> false
                     | _ -> false
                match blockPropogation, nextTarget with
                |true, _ -> VSConstants.S_OK
                |false, Some(target) -> target.Exec(&commandGroup, commandID, opt, pvaIn, pvaOut)
                |_, None -> VSConstants.S_OK

            member this.QueryStatus (group, cmds, prgCmds, cmdText) =
                match nextTarget with
                |Some(target) -> target.QueryStatus(&group, cmds, prgCmds, cmdText)
                |None -> VSConstants.S_OK
