package com.nxmup.androidclient.service;

import android.app.Service;
import android.content.Intent;
import android.os.Binder;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;

import com.nxmup.androidclient.listener.OnStateChangeListener;
import com.nxmup.androidclient.util.HttpUtil;
import com.nxmup.androidclient.util.LogUtil;
import com.nxmup.androidclient.util.PreferenceUtil;
import com.nxmup.androidclient.util.UrlBuilder;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.Response;

public class StateService extends Service {

    private String currentState = null;

    private Handler mHandler = new Handler(Looper.getMainLooper());
    private OnStateChangeListener onStateChangeListener;
    private String id;
    private String password;

    public StateService() {
    }

    public void setOnStateChangeListener(OnStateChangeListener onStateChangeListener) {
        this.onStateChangeListener = onStateChangeListener;
    }

    @Override
    public IBinder onBind(Intent intent) {
        return new SingleHolder();
    }

    public class SingleHolder extends Binder {
        public StateService getStateService() {
            return StateService.this;
        }
    }

    private Runnable runnable = new Runnable() {
        @Override
        public void run() {
            Callback callback = new Callback() {
                @Override
                public void onFailure(Call call, IOException e) {

                }

                @Override
                public void onResponse(Call call, Response response) throws IOException {
                    String json = response.body().string();
                    try {
                        JSONObject jsonObject = new JSONObject(json);
                        JSONObject state = jsonObject.optJSONObject("state");
                        String userId = jsonObject.optString("userId");
                        id = userId;
                        String newState = state.optString("state");
                        onStateChangeListener.onStateChange(newState);
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }
                }
            };
            HttpUtil.updateState(PreferenceUtil.getToken(), callback);
            mHandler.postDelayed(runnable, 5000);
        }
    };

    public void updateStateConstantly() {
        mHandler.removeCallbacks(runnable);
        mHandler.post(runnable);
    }

    public void stopUpdateState() {
        mHandler.removeCallbacks(runnable);
    }

    public void setCurrentIdAndPassword(String id, String password) {
        this.id = id;
        this.password = password;
    }

    public String getCurrentId() {
        return id;
    }

    public String getCurrentState() {
        return currentState;
    }

    public void setCurrentState(String currentState) {
        this.currentState = currentState;
    }

    public void saveToken() {
        HttpUtil.getToken(id, password, new Callback() {
            @Override
            public void onFailure(Call call, IOException e) {

            }

            @Override
            public void onResponse(Call call, Response response) throws IOException {
                String json = response.body().string();
                try {
                    JSONObject jsonObject = new JSONObject(json);
                    String token = jsonObject.optString("token");
                    PreferenceUtil.saveToken(token);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
    }
}
