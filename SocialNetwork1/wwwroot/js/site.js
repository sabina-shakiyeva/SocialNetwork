﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


function GetAllUsers() {
    $.ajax({
        url: "/Home/GetAllUsers",
        method: "GET",
        success: function (data) {
            let content = "";
            for (var i = 0; i < data.length; i++) {
                let style = '';
                let subContent = '';
                if (data[i].hasRequestPending) {
                    subContent = `<button class='btn btn-outline-secondary' onclick="TakeRequest('${data[i].id}')">Already Sent</button>`
                }
                else {
                    if (data[i].isFriend) {
                        subContent = `<button class='btn btn-outline-secondary' onclick="SendFollow('${data[i].id}')" >UnFollow</button>
                        <a class='btn btn-outline-success' href='Home/GoChat/${data[i].id}'>Send Message</a>
                        `;
                    }
                    else {
                        subContent = `<button class='btn btn-outline-primary' onclick="SendFollow('${data[i].id}')" >Follow</button>`;
                    }
                }
                if (data[i].isOnline) {
                    style = "border:5px solid springgreen";
                }
                else {
                    style = "border:5px solid red";
                }
                const item = `
                    <div class='card' style='${style};width:220px:margin:5px'>

                    <img style='width:100%;height:220px' src='/images/${data[i].image}' />
                    <div class='card-body'>
                        <h5 class='card-title'>${data[i].userName}</h5>
                        <p class='card-text'>${data[i].email}</p>
                    </div>

                    ${subContent}

                    </div>
                `;

                content += item;
            }

            $("#allUsers").html(content);

        }
    })
}

function DeclineRequest(id, senderId) {
    $.ajax({
        url: `/Home/DeclineRequest?id=${id}&senderId=${senderId}`,
        method: "GET",
        success: function (data) {
            const element = document.querySelector("#alert");
            element.style.display = "block";
            element.innerHTML = "You declined request";

            SendFollowCall(senderId);
            GetMyRequests();
            GetAllUsers();


            setTimeOut(() => {
                element.innerHTML = "";
                element.style.display = "none";
            }, 5000);
        }
    })
}

function GetMyRequests() {
    $.ajax({
        url: "/Home/GetAllRequests",
        method: "GET",
        success: function (data) {
            $("#requests").html("");
            let content = "";
            let subContent = "";
            for (let i = 0; i < data.length; i++) {
                if (data[i].status == "Request") {
                    subContent = `
                    <div class='card-body'>
                    <button class='btn btn-success' onclick="AcceptRequest('${data[i].senderId}','${data[i].receiverId}',${data[i].id})" >Accept</button>
                    <button class='btn btn-warning' onclick="DeclineRequest(${data[i].id},'${data[i].senderId}')" >Decline</button>
                    </div>
                    `;
                }
                else {
                    subContent = `
                    <div class='card-body'>
                    <button class='btn btn-warning' onclick="DeleteRequest(${data[i].id})" >Delete</button>
                    </div>
                    `;
                }

                let item = `
                    <div class='card' style='width:15rem;'>
                    <div class='card-body'>
                        <h5>Request</h5>
                        <ul class='list-group list-group-flush'>
                            <li>${data[i].content}</li>
                        </ul>
                        ${subContent}
                    </div>
                    </div>
                `;

                content += item;

            }

            $("#requests").html(content);
        }
    })
}

GetMyRequests();
GetAllUsers();


function GetMessages(currentId, receiverId, senderId) {
    console.log("CurrentId", currentId);
    console.log("senderId", senderId);
    console.log("receiverId", receiverId);
    $.ajax({
        url: `/Home/GetAllMessages?receiverId=${receiverId}&senderId=${senderId}`,
        method: 'GET',
        success: function (data) {
            let content = "";
            for (var i = 0; i < data.messages.length; i++) {

                let item = ``;

                if (senderId == data.messages[i].senderId) {
                    item = ` <section style="display:flex;margin-top:25px;border:4px solid springgreen;
margin-left:150px;border-radius:20px 0 0 20px;padding:20px;width:50%">

                <h5>${data.messages[i].content}</h5>
                <p>${data.messages[i].dateTime}</p>
            </section>`;

                }
                else {
                    item = `  <section style="display:flex;margin-top:25px;border:4px solid deepskyblue;
margin-left:0;border-radius:0 20px 20px 0;padding:20px;width:50%">

                <h5>${data.messages[i].content}</h5>
                <p>${data.messages[i].dateTime}</p>
            </section>
            `;
                }

                content += item;
            }

            $("#currentMessages").html(content);
        }
    })
}

function SendMessage(receiverId, senderId) {
    const content = document.querySelector("#message-input");
    let obj = {
        receiverId: receiverId,
        senderId: senderId,
        content: content.value
    };

    $.ajax({
        url: `/Home/AddMessage`,
        method: "POST",
        data: obj,
        success: function (data) {
            GetMessageCall(receiverId,senderId);
            content.value = "";
        }
    })
}


function DeleteRequest(id) {
    $.ajax({
        url: `/Home/DeleteRequest/${id}`,
        method: "GET",
        success: function (data) {

            GetMyRequests();

        }
    })
}

function SendFollow(id) {
    $.ajax({
        url: `/Home/SendFollow/${id}`,
        method: "GET",
        success: function (data) {
            const element = document.querySelector("#alert");
            element.style.display = "block";
            element.innerHTML = "Your friend request sent successfully";
            SendFollowCall(id);
            GetAllUsers();

            setTimeout(() => {
                element.innerHTML = "";
                element.style.display = "none";
            }, 5000);
        }
    })
}

function AcceptRequest(id, id2, requestId) {
    $.ajax({
        url: `/Home/AcceptRequest?senderId=${id}&receiverId=${id2}&requestId=${requestId}`,
        method: "GET",
        success: function (data) {
            const element = document.querySelector("#alert");
            element.style.display = "block";
            element.innerHTML = "Your accept friend request successfully";
            GetAllUsers();
            SendFollowCall(id);
            SendFollowCall(id2);

            setTimeout(() => {
                element.innerHTML = "";
                element.style.display = "none";
            }, 5000);
        }
    })
}

function TakeRequest(id) {
    $.ajax({
        url: `/Home/TakeRequest/${id}`,
        method: "DELETE",
        success: function (data) {
            const element = document.querySelector("#alert");
            element.style.display = "block";
            element.innerHTML = "You has taken your request successfully successfully";
            SendFollowCall(id);
            GetAllUsers();

            setTimeout(() => {
                element.innerHTML = "";
                element.style.display = "none";
            }, 5000);
        }
    })
}