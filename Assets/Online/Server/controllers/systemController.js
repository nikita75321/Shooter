async function handleDemoRequest(ws, data) {
    const client = await pool.connect();
    try {        
        ws.send(JSON.stringify({
            action: 'demo_response',
            demo_massage: true
        }));
    } catch (error) {
        console.error('Error in demoRequest:', error);
        sendError(ws, 'Failed to demoRequest');
    } finally {
        client.release();
    }
}

async function handleGetTimeUntilMonthEnd(ws) {
    try {
        const now = new Date();
        const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59);
        const diffMs = endOfMonth - now;
        
        const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        const hours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
        
        ws.send(JSON.stringify({
            action: 'time_until_month_end_response',
            days: days,
            hours: hours,
            minutes: minutes
        }));
    } catch (error) {
        console.error('Error in handleGetTimeUntilMonthEnd:', error);
        sendError(ws, 'Failed to calculate time until month end');
    }
}

async function handleGetGameModesStatus(ws) {
    const now = Date.now();
    
    const mode2Cycle = 75 * 1000;
    const mode2CyclePos = now % mode2Cycle;
    const mode2Available = mode2CyclePos < (30 * 1000);
    const mode2TimeLeft = mode2Available 
        ? (30 * 1000 - mode2CyclePos) / 1000 
        : (75 * 1000 - mode2CyclePos) / 1000;

    const mode3Cycle = 105 * 1000;
    const mode3CyclePos = now % mode3Cycle;
    const mode3Available = mode3CyclePos < (45 * 1000);
    const mode3TimeLeft = mode3Available 
        ? (45 * 1000 - mode3CyclePos) / 1000 
        : (105 * 1000 - mode3CyclePos) / 1000;

    const response = {
        action: 'game_modes_status_response',
        modes: {
            mode1: { available: true, timeLeft: 0 },
            mode2: { available: mode2Available, timeLeft: mode2TimeLeft },
            mode3: { available: mode3Available, timeLeft: mode3TimeLeft }
        },
        serverTime: now
    };
    
    ws.send(JSON.stringify(response));
}

module.exports = {
    handleDemoRequest,
    handleGetTimeUntilMonthEnd,
    handleGetGameModesStatus
};