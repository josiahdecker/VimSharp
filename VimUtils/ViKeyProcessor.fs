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
        ///active at the time. 
        override this.PreviewKeyUp args =
            match (args.Key, context.Mode) with
                | Key.Tab, Some(mode) ->
                    //Return true with HandleTab to stop default processing 
                    if not <| mode.HandleTab context then
                        base.PreviewKeyUp args
                |Key.Enter, Some(mode) ->
                    if not <| mode.HandleEnter context then
                        base.PreviewKeyUp args
                | _, _ -> base.PreviewKeyUp args

        member this.EscapeCommandSent () = 
            if not <| intellisenseMonitor.IsIntellisenseActive() then
                context.Operations.ResetSelection()
                context.SetBaseMode()
            else
                intellisenseMonitor.DeactivateIntellisense()

    and EscapeCommandFilter(processor: ViKeyProcessor, viewAdapter: IVsTextView) =
        let mutable nextTarget: IOleCommandTarget option = None

        member this.NextTarget with 
                                    get() = nextTarget
                                    and set(value) = nextTarget <- value 

        member this.Disconnect () =
            viewAdapter.RemoveCommandFilter(this)

        interface IOleCommandTarget with
            member this.Exec(commandGroup, commandID, opt, pvaIn, pvaOut) =
                if commandGroup = VSConstants.VSStd2K then
                    if  commandID = (uint32 VSConstants.VSStd2KCmdID.CANCEL) || 
                        commandID = (uint32 VSConstants.VSStd2KCmdID.Cancel) then
                            processor.EscapeCommandSent()
                match nextTarget with
                |Some(target) -> target.Exec(&commandGroup, commandID, opt, pvaIn, pvaOut)
                |None -> VSConstants.S_OK;

            member this.QueryStatus (group, cmds, prgCmds, cmdText) =
                match nextTarget with
                |Some(target) -> target.QueryStatus(&group, cmds, prgCmds, cmdText)
                |None -> VSConstants.S_OK
