// Search hierarchy for Unit_Player and Unit_Enemy with component details
const WebSocket = require('C:/Users/madde/Bounty_Of_The_Deathfeather_V3/Library/PackageCache/com.gamelovers.mcp-unity@0d4643656803/Server~/node_modules/ws');

const ws = new WebSocket('ws://localhost:8090/McpUnity');

function searchHierarchy(obj, path = '') {
    if (!obj) return;
    
    const currentPath = path ? `${path}/${obj.name}` : obj.name;
    
    // Check if this is one of our units
    if (obj.name === 'Unit_Player' || obj.name === 'Unit_Enemy' || obj.name === 'UnitManager') {
        console.log(`\n=== Found: ${obj.name} ===`);
        console.log(`Path: ${currentPath}`);
        console.log(`Active: ${obj.activeSelf}`);
        console.log(`Components:`);
        if (obj.components) {
            obj.components.forEach(comp => {
                console.log(`  - ${comp.type} (enabled: ${comp.enabled})`);
            });
        }
    }
    
    // Recurse into children
    if (obj.children && obj.children.length > 0) {
        obj.children.forEach(child => searchHierarchy(child, currentPath));
    }
}

ws.on('open', () => {
    const payload = {
        id: 'get_hierarchy',
        method: 'get_scenes_hierarchy',
        params: {}
    };
    ws.send(JSON.stringify(payload));
});

ws.on('message', (data) => {
    const response = JSON.parse(data.toString());
    if (response.result && response.result.scenes) {
        response.result.scenes.forEach(scene => {
            scene.rootGameObjects.forEach(root => {
                searchHierarchy(root);
            });
        });
    }
    ws.close();
});

ws.on('error', (error) => {
    console.error('MCP Unity server error:', error.message);
    process.exit(1);
});
