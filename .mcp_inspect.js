const WebSocket = require('ws');
const ws = new WebSocket('ws://localhost:8090/McpUnity');

const payloads = [
  { id: 'assets1', method: 'query_assets', params: { path: 'Assets/TBSFramework/Examples/TilemapExample', recursive: true } },
  { id: 'hier', method: 'get_scene_hierarchy', params: { includeInactive: true } },
  { id: 'findTilemap', method: 'find_components', params: { componentName: 'TilemapCellManager' } },
  { id: 'findGridCtrl', method: 'find_components', params: { componentName: 'UnityGridController' } },
  { id: 'dataTiles', method: 'query_assets', params: { path: 'Assets', filter: 't:DataTile', recursive: true } }
];

ws.on('open', () => {
  console.log('MCP connected');
  let i = 0;
  ws.on('message', (m) => {
    try {
      console.log('\n<<< RESPONSE\n' + m.toString());
    } catch (e) {
      console.error('print error', e);
    }
    i++;
    if (i < payloads.length) {
      ws.send(JSON.stringify(payloads[i]));
    } else {
      ws.close();
    }
  });
  ws.send(JSON.stringify(payloads[0]));
});

ws.on('error', (e) => {
  console.error('WS error', e);
});

ws.on('close', () => {
  console.log('MCP connection closed');
});
