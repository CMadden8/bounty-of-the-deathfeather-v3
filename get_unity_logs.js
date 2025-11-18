// Get Unity console logs
const WebSocket = require('C:/Users/madde/Bounty_Of_The_Deathfeather_V3/Library/PackageCache/com.gamelovers.mcp-unity@0d4643656803/Server~/node_modules/ws');

const ws = new WebSocket('ws://localhost:8090/McpUnity');

ws.on('open', () => {
    const payload = {
        id: 'logs',
        method: 'get_console_logs',
        params: {
            logType: null,
            offset: 0,
            limit: 50,
            includeStackTrace: false
        }
    };
    ws.send(JSON.stringify(payload));
});

ws.on('message', (data) => {
    const response = JSON.parse(data.toString());
    console.log(JSON.stringify(response, null, 2));
    ws.close();
});

ws.on('error', (error) => {
    console.error('MCP Unity server error:', error.message);
    process.exit(1);
});
