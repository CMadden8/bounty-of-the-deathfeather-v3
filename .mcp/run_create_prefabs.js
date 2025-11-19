const WebSocket = require('ws');
const ws = new WebSocket('ws://localhost:8090/McpUnity');

ws.on('open', () => {
  console.log('Connected to MCP Unity server.');

  const makePayload = (path, id) => ({ id: id, method: 'execute_menu_item', params: { menuPath: path } });
  const items = [
    makePayload('Tools/Combat POC/Create Simple 3D Cell Prefab (Gray)', 'gray'),
    makePayload('Tools/Combat POC/Create Simple 3D Cell Prefab (Blue)', 'blue'),
    makePayload('Tools/Combat POC/Create Simple 3D Cell Prefab (Green)', 'green')
  ];

  let i = 0;
  const sendNext = () => {
    if (i >= items.length) {
      console.log('All menu items sent. Waiting briefly and closing.');
      setTimeout(() => ws.close(), 500);
      return;
    }
    console.log('Sending menu:', items[i].params.menuPath);
    ws.send(JSON.stringify(items[i]));
    i++;
    setTimeout(sendNext, 700);
  };

  sendNext();
});

ws.on('message', (data) => {
  console.log('MCP:', data.toString());
});

ws.on('error', (err) => {
  console.error('WebSocket error:', err);
  process.exit(1);
});

ws.on('close', () => {
  console.log('Connection closed.');
});
