(function () {
    let client = null;
    let localAudioTrack = null;
    let localVideoTrack = null;
    let joined = false;

    function setText(id, text) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = text;
        }
    }

    function ensureAgora() {
        if (!window.AgoraRTC) {
            throw new Error("Agora Web SDK is not loaded.");
        }
    }

    async function cleanup() {
        if (localAudioTrack) {
            localAudioTrack.close();
            localAudioTrack = null;
        }

        if (localVideoTrack) {
            localVideoTrack.close();
            localVideoTrack = null;
        }

        if (client && joined) {
            await client.leave();
        }

        joined = false;
        client = null;

        const local = document.getElementById("local-player");
        const remote = document.getElementById("remote-player");
        if (local) {
            local.innerHTML = "";
        }
        if (remote) {
            remote.innerHTML = "";
        }
    }

    window.bikeMateEmergencyCall = {
        async join(session) {
            ensureAgora();
            await cleanup();

            if (!session || !session.appId || !session.channelName || !session.token || session.uid == null) {
                throw new Error(session?.message || "Agora session is missing app id, token, channel, or uid.");
            }

            client = AgoraRTC.createClient({ mode: "rtc", codec: "vp8" });
            client.on("user-published", async (user, mediaType) => {
                await client.subscribe(user, mediaType);
                if (mediaType === "video" && user.videoTrack) {
                    const remote = document.getElementById("remote-player");
                    if (remote) {
                        remote.innerHTML = "";
                        user.videoTrack.play(remote);
                    }
                    setText("remote-status", "Remote video connected");
                }

                if (mediaType === "audio" && user.audioTrack) {
                    user.audioTrack.play();
                    setText("remote-audio-status", "Remote audio connected");
                }
            });

            client.on("user-unpublished", (_, mediaType) => {
                if (mediaType === "video") {
                    const remote = document.getElementById("remote-player");
                    if (remote) {
                        remote.innerHTML = "";
                    }
                    setText("remote-status", "Remote camera is off");
                }
            });

            client.on("user-left", () => {
                const remote = document.getElementById("remote-player");
                if (remote) {
                    remote.innerHTML = "";
                }
                setText("remote-status", "Remote participant left");
            });

            setText("call-status", "Requesting camera and microphone...");
            [localAudioTrack, localVideoTrack] = await AgoraRTC.createMicrophoneAndCameraTracks();
            await client.join(session.appId, session.channelName, session.token, Number(session.uid));
            joined = true;

            const local = document.getElementById("local-player");
            if (local) {
                local.innerHTML = "";
                localVideoTrack.play(local);
            }

            await client.publish([localAudioTrack, localVideoTrack]);
            setText("call-status", `Connected to ${session.channelName}`);
            setText("remote-status", "Waiting for customer or responder video...");
            setText("remote-audio-status", "Waiting for remote audio...");
            return true;
        },

        async leave() {
            await cleanup();
            setText("call-status", "Call ended");
            setText("remote-status", "No remote video");
            setText("remote-audio-status", "No remote audio");
            return true;
        },

        async setMuted(muted) {
            if (localAudioTrack) {
                await localAudioTrack.setEnabled(!muted);
            }
            return muted;
        },

        async setCamera(enabled) {
            if (localVideoTrack) {
                await localVideoTrack.setEnabled(enabled);
            }
            return enabled;
        }
    };
})();
