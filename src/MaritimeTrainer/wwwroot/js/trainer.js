/* Maritime Trainer – browser helpers: Morse audio (Web Audio), animated lights, keyboard hooks. */
window.trainer = (() => {
    'use strict';

    // ---------------------------------------------------------------- Morse timing (pure, unit-testable)
    /**
     * Turns a pattern into tone events.
     * Pattern grammar: '.' dit, '-' dah, ' ' letter gap, '/' word gap.
     * charWpm = speed of the characters themselves; effWpm = overall speed (Farnsworth spacing when lower).
     * Returns { events: [{t, d}], total } in seconds.
     */
    function schedule(pattern, charWpm, effWpm) {
        charWpm = Math.max(1, charWpm || 18);
        effWpm = Math.min(Math.max(1, effWpm || charWpm), charWpm);
        const unit = 1.2 / charWpm;                       // seconds per dit ("PARIS" standard)
        // Farnsworth: stretch only the gaps so the overall rate equals effWpm.
        const gapUnit = effWpm >= charWpm ? unit : (60 / effWpm - 37.2 / charWpm) / 19;
        const events = [];
        let t = 0;
        let lastWasElement = false;
        for (const ch of pattern) {
            if (ch === '.' || ch === '-') {
                if (lastWasElement) t += unit;            // gap inside a letter
                const d = (ch === '.' ? 1 : 3) * unit;
                events.push({ t, d });
                t += d;
                lastWasElement = true;
            } else if (ch === ' ') {
                t += 3 * gapUnit;                         // gap between letters
                lastWasElement = false;
            } else if (ch === '/') {
                t += 7 * gapUnit;                         // gap between words
                lastWasElement = false;
            }
        }
        return { events, total: t };
    }

    // ---------------------------------------------------------------- Web Audio
    let ctx = null, master = null, active = [], endTimer = null, endResolve = null;

    function ensure() {
        if (!ctx) {
            const AC = window.AudioContext || window.webkitAudioContext;
            ctx = new AC();
            master = ctx.createGain();
            master.connect(ctx.destination);
        }
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    }

    // Browsers only start audio after a gesture: prepare it on the first tap/key press anywhere.
    const unlock = () => { try { ensure(); } catch (e) { /* no audio available */ } };
    window.addEventListener('pointerdown', unlock, { once: true, capture: true });
    window.addEventListener('keydown', unlock, { once: true, capture: true });

    function stop() {
        for (const o of active) { try { o.stop(); } catch (e) { /* already stopped */ } }
        active = [];
        if (endTimer) { clearTimeout(endTimer); endTimer = null; }
        if (endResolve) { const r = endResolve; endResolve = null; r(); }
    }

    function playMorse(pattern, charWpm, effWpm, freq, volume) {
        stop();
        let c;
        try { c = ensure(); } catch (e) { return Promise.resolve(); }
        const ev = schedule(pattern, charWpm, effWpm);
        const t0 = c.currentTime + 0.08;
        master.gain.cancelScheduledValues(c.currentTime);
        master.gain.setValueAtTime(volume, c.currentTime);
        const ramp = 0.006;
        for (const e of ev.events) {
            const osc = c.createOscillator();
            const g = c.createGain();
            osc.type = 'sine';
            osc.frequency.value = freq;
            const a = t0 + e.t, b = a + e.d;
            g.gain.setValueAtTime(0, a);
            g.gain.linearRampToValueAtTime(1, a + ramp);
            g.gain.setValueAtTime(1, Math.max(a + ramp, b - ramp));
            g.gain.linearRampToValueAtTime(0, b);
            osc.connect(g); g.connect(master);
            osc.start(a); osc.stop(b + 0.02);
            active.push(osc);
        }
        return new Promise(resolve => {
            endResolve = resolve;
            endTimer = setTimeout(() => { endTimer = null; endResolve = null; active = []; resolve(); },
                (ev.total + 0.2) * 1000);
        });
    }

    function tone(freq, ms, volume) {
        let c;
        try { c = ensure(); } catch (e) { return; }
        const now = c.currentTime;
        const osc = c.createOscillator(), g = c.createGain();
        osc.frequency.value = freq;
        g.gain.setValueAtTime(0, now);
        g.gain.linearRampToValueAtTime(volume, now + 0.006);
        g.gain.setValueAtTime(volume, now + ms / 1000 - 0.006);
        g.gain.linearRampToValueAtTime(0, now + ms / 1000);
        osc.connect(g); g.connect(c.destination);
        osc.start(now); osc.stop(now + ms / 1000 + 0.02);
    }

    function chime(correct, volume) {
        let c;
        try { c = ensure(); } catch (e) { return; }
        const now = c.currentTime;
        const notes = correct ? [[660, 0, 0.09], [880, 0.09, 0.16]] : [[190, 0, 0.22]];
        for (const [f, off, dur] of notes) {
            const osc = c.createOscillator(), g = c.createGain();
            osc.type = correct ? 'triangle' : 'sawtooth';
            osc.frequency.value = f;
            const v = volume * (correct ? 0.5 : 0.28);
            g.gain.setValueAtTime(0, now + off);
            g.gain.linearRampToValueAtTime(v, now + off + 0.01);
            g.gain.exponentialRampToValueAtTime(0.001, now + off + dur);
            osc.connect(g); g.connect(c.destination);
            osc.start(now + off); osc.stop(now + off + dur + 0.02);
        }
    }

    // ---------------------------------------------------------------- Animated lights
    // Each light element gets CSS variables: --lc (colour) and --lv (intensity); class "on" while lit.
    // A lamp and its time band are registered in the same render, so starts that land within a
    // moment of each other share one start time – they then stay exactly in step.
    const lights = new Map();
    const timelines = new Map();
    let raf = 0, lastStart = -1e9;

    function startTime() {
        const now = performance.now();
        if (now - lastStart > 250) lastStart = now;
        return lastStart;
    }

    function lightLoop(now) {
        for (const [el, s] of lights) {
            if (!el.isConnected) { lights.delete(el); continue; }
            const t = (Math.max(0, now - s.start) / 1000) % s.total;
            let acc = 0, idx = 0;
            for (; idx < s.durs.length; idx++) {
                acc += s.durs[idx];
                if (t < acc) break;
            }
            if (idx >= s.durs.length) idx = s.durs.length - 1;
            if (idx !== s.idx) {
                s.idx = idx;
                const colour = s.cols[idx];
                el.style.setProperty('--lc', colour || 'transparent');
                el.style.setProperty('--lv', String(s.levels[idx]));
                el.classList.toggle('on', !!colour);
            }
        }
        for (const [el, s] of timelines) {
            if (!el.isConnected) { timelines.delete(el); continue; }
            const t = (Math.max(0, now - s.start) / 1000) % s.window;
            s.head.style.left = (t / s.window * 100).toFixed(3) + '%';
        }
        raf = (lights.size || timelines.size) ? requestAnimationFrame(lightLoop) : 0;
    }

    function kick() { if (!raf) raf = requestAnimationFrame(lightLoop); }

    function startLight(el, durs, cols, levels) {
        if (!el) return;
        const total = durs.reduce((a, b) => a + b, 0);
        lights.set(el, { durs, cols, levels, total, start: startTime(), idx: -1 });
        kick();
    }

    function stopLight(el) {
        lights.delete(el);
    }

    /** el = a time band's track (holds a .ltl-head); window = seconds shown across the band. */
    function startTimeline(el, period, window) {
        if (!el) return;
        const head = el.querySelector('.ltl-head');
        if (!head) return;
        timelines.set(el, { head, period, window, start: startTime() });
        kick();
    }

    function stopTimeline(el) {
        timelines.delete(el);
    }

    // ---------------------------------------------------------------- Keyboard hook
    let keyHandler = null;

    function registerKeys(dotnetRef) {
        unregisterKeys();
        keyHandler = (e) => {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            const tag = (e.target && e.target.tagName) || '';
            if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
            // Focused buttons / links / role=button cards activate themselves on Enter or Space.
            const role = e.target && e.target.getAttribute && e.target.getAttribute('role');
            if ((tag === 'BUTTON' || tag === 'A' || role === 'button') && (e.key === 'Enter' || e.key === ' ')) return;
            dotnetRef.invokeMethodAsync('OnKey', e.key).catch(() => { });
            if (e.key === ' ') e.preventDefault();   // keep Space from scrolling the page
        };
        window.addEventListener('keydown', keyHandler);
    }

    function unregisterKeys() {
        if (keyHandler) { window.removeEventListener('keydown', keyHandler); keyHandler = null; }
    }

    function blur() { if (document.activeElement && document.activeElement.blur) document.activeElement.blur(); }
    function focusById(id) { const el = document.getElementById(id); if (el) el.focus(); }

    const api = { schedule, playMorse, stop, tone, chime, startLight, stopLight, startTimeline, stopTimeline, registerKeys, unregisterKeys, blur, focusById };
    if (typeof module !== 'undefined' && module.exports) module.exports = api;   // for node tests
    return api;
})();
