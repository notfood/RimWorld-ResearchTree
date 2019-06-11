﻿using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using Verse;

namespace FluffyResearchTree
{
    // Can be run anywhere really. Multiplayer runs its API init code on Mod()
    // and since it runs always after Core, it's certain it will be ready.
    [StaticConstructorOnStartup]
    public static class Multiplayer
    {
        static Dictionary<ResearchProjectDef, ResearchNode> _cache;

        static Multiplayer()
        {
            if ( !MP.enabled ) return;

            // Let's sync all Queue operations that involve the GUI
            // Don't worry about EnqueueRange calling Enqueue, MP mod handles it
            MP.RegisterSyncMethod( typeof( Queue ), nameof( Queue.Enqueue ));
            MP.RegisterSyncMethod( typeof( Queue ), nameof( Queue.EnqueueRange ));
            MP.RegisterSyncMethod( typeof( Queue ), nameof( Queue.Dequeue ));

            // Sorry Fluffy, you can't save yourself from this one in MP
            // Tree.Nodes must exist for all players for the sync commands to be valid
            Tree.Initialize();
        }

        static ResearchNode Lookup( ResearchProjectDef projectDef )
        {
            if (_cache == null)
                _cache = Tree.Nodes.OfType<ResearchNode>().ToDictionary( r => r.Research );

            return _cache[projectDef];
        }

        // We only care about the research, no new nodes will be created or moved.
        [SyncWorker( shouldConstruct = false )]
        static void HandleResearchNode( SyncWorker sw, ref ResearchNode node )
        {
            // Bind commands are in the order they are placed
            // So if you write a Def first, you must read it first and so on
            if ( sw.isWriting )
            {
                // We are writing, node is the outgoing object
                sw.Bind( ref node.Research );
            }
            else
            {
                ResearchProjectDef research = null;

                sw.Bind( ref research );

                // We are reading, node is null, we must set it. So we look it up
                // research is unique in the Tree
                node = Lookup(research);
            }
        }
    }
}
